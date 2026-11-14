using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Servicios;
using SistemaTaller.Api.Features.Stock;
using SistemaTaller.Api.Features.Ventas;

namespace SistemaTaller.Tests;

/// <summary>Fase 5 (catálogo de servicios) y Fase 6 (stock y ventas).</summary>
[Collection("api")]
public class CatalogoStockVentasTests(SistemaTallerFactory fabrica)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static async Task<T> Leer<T>(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<T>(Json))!;

    private async Task<(Guid Taller, Guid Vehiculo, HttpClient Cliente)> Taller(string patente)
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo(patente);
        return (taller, vehiculo, fabrica.CrearClienteAutenticadoComo(taller));
    }

    private static async Task<ProductoRespuesta> CrearProducto(HttpClient c, string nombre, decimal precio, int stock) =>
        await Leer<ProductoRespuesta>(await c.PostAsJsonAsync("/api/productos", new { nombre, precio, stockActual = stock }, Json));

    // ================= Fase 5: catálogo =================

    [Fact]
    public async Task ServicioDesactivadoNoApareceEnElSelectorPeroSigueVisibleEnOrdenesViejas()
    {
        var (_, vehiculo, cliente) = await Taller("CA5001");
        var cat = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios?soloActivos=true", Json))!;
        var rotacion = cat.Single(s => s.Nombre == "Rotación");
        var orden = await Leer<OrdenDetalle>(await cliente.PostAsJsonAsync("/api/ordenes",
            new { vehiculoId = vehiculo, servicios = new[] { new { servicioId = rotacion.Id } } }, Json));

        var r = await cliente.PatchAsJsonAsync($"/api/servicios/{rotacion.Id}", new { activo = false }, Json);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var selector = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios?soloActivos=true", Json))!;
        var catalogoCompleto = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!;
        var ordenVieja = await cliente.GetFromJsonAsync<OrdenDetalle>($"/api/ordenes/{orden.Id}", Json);
        var ordenNueva = await cliente.PostAsJsonAsync("/api/ordenes",
            new { vehiculoId = vehiculo, servicios = new[] { new { servicioId = rotacion.Id } } }, Json);

        Assert.DoesNotContain(selector, s => s.Id == rotacion.Id);
        Assert.False(catalogoCompleto.Single(s => s.Id == rotacion.Id).Activo); // sigue en el catálogo, inactivo
        Assert.Contains(ordenVieja!.Servicios, s => s.Servicio == "Rotación");
        Assert.Equal(HttpStatusCode.BadRequest, ordenNueva.StatusCode);
    }

    [Fact]
    public async Task SePuedeReactivarUnServicioYCambiarOQuitarSuPrecioDeReferencia()
    {
        var (_, _, cliente) = await Taller("CA5002");
        var balanceo = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!.Single(s => s.Nombre == "Balanceo");

        await cliente.PatchAsJsonAsync($"/api/servicios/{balanceo.Id}", new { activo = false }, Json);
        var reactivado = await Leer<ServicioRespuesta>(await cliente.PatchAsJsonAsync($"/api/servicios/{balanceo.Id}", new { activo = true }, Json));
        var conPrecio = await Leer<ServicioRespuesta>(await cliente.PatchAsJsonAsync($"/api/servicios/{balanceo.Id}", new { precioReferencia = 21000m }, Json));
        var sinPrecio = await Leer<ServicioRespuesta>(await cliente.PatchAsJsonAsync($"/api/servicios/{balanceo.Id}", new { sinPrecio = true }, Json));

        Assert.True(reactivado.Activo);
        Assert.Equal(21000m, conPrecio.PrecioReferencia);
        Assert.True(conPrecio.Activo); // cambiar el precio no toca el estado
        Assert.Null(sinPrecio.PrecioReferencia);
    }

    [Fact]
    public async Task AgregarUnServicioPropioLoDejaDisponibleEnElSelector()
    {
        var (_, _, cliente) = await Taller("CA5003");

        var r = await cliente.PostAsJsonAsync("/api/servicios", new
        {
            nombre = "  Cambio de aceite ", categoriaVehiculo = "Ambos", aplicaPosicion = false, aplicaLado = false, precioReferencia = 25000m,
        }, Json);

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var creado = await Leer<ServicioRespuesta>(r);
        Assert.Equal("Cambio de aceite", creado.Nombre);
        var selector = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios?soloActivos=true", Json))!;
        Assert.Contains(selector, s => s.Id == creado.Id && s.PrecioReferencia == 25000m);
    }

    [Fact]
    public async Task AltaDeServicioInvalidaODuplicadaDa400()
    {
        var (_, _, cliente) = await Taller("CA5004");

        var sinNombre = await cliente.PostAsJsonAsync("/api/servicios", new { nombre = "", categoriaVehiculo = "Auto" }, Json);
        var sinCategoria = await cliente.PostAsJsonAsync("/api/servicios", new { nombre = "Pulido" }, Json);
        var duplicado = await cliente.PostAsJsonAsync("/api/servicios", new { nombre = "balanceo", categoriaVehiculo = "Auto" }, Json);
        var precioNegativo = await cliente.PostAsJsonAsync("/api/servicios", new { nombre = "Pulido", categoriaVehiculo = "Auto", precioReferencia = -1 }, Json);

        Assert.All(new[] { sinNombre, sinCategoria, duplicado, precioNegativo }, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task NoExisteDeleteDeServicios()
    {
        var (_, _, cliente) = await Taller("CA5005");
        var uno = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))![0];

        var r = await cliente.DeleteAsync($"/api/servicios/{uno.Id}");

        // No hay ruta DELETE: responde 404/405 según el enrutado, y el servicio sigue existiendo.
        Assert.Contains(r.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        var catalogo = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!;
        Assert.Contains(catalogo, s => s.Id == uno.Id);
    }

    [Fact]
    public async Task ElCatalogoEsDeCadaTaller()
    {
        var (_, _, clienteA) = await Taller("CA5006");
        var (tallerB, _, clienteB) = await Taller("CB5006");
        var deB = (await clienteB.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!.Single(s => s.Nombre == "Balanceo");
        var propioDeA = await Leer<ServicioRespuesta>(await clienteA.PostAsJsonAsync("/api/servicios",
            new { nombre = "Solo de A", categoriaVehiculo = "Auto", tallerId = tallerB }, Json));

        var ajeno = await clienteA.PatchAsJsonAsync($"/api/servicios/{deB.Id}", new { activo = false }, Json);
        var catalogoB = (await clienteB.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!;

        Assert.Equal(HttpStatusCode.NotFound, ajeno.StatusCode);
        Assert.True(catalogoB.Single(s => s.Id == deB.Id).Activo);
        Assert.DoesNotContain(catalogoB, s => s.Id == propioDeA.Id); // el tallerId del body se ignoró
    }

    // ================= Fase 6: stock =================

    [Fact]
    public async Task AltaEdicionBajaYConsultaDeProductos()
    {
        var (_, _, cliente) = await Taller("ST6001");

        var creado = await CrearProducto(cliente, " Aceite 15W40 · 4 L ", 28500m, 6);
        Assert.Equal("Aceite 15W40 · 4 L", creado.Nombre);

        var editado = await Leer<ProductoRespuesta>(await cliente.PutAsJsonAsync($"/api/productos/{creado.Id}",
            new { nombre = "Aceite 15W40 · 4 L", precio = 30000m, stockActual = 10 }, Json));
        Assert.Equal(30000m, editado.Precio);
        Assert.Equal(10, editado.StockActual);

        var baja = await cliente.DeleteAsync($"/api/productos/{creado.Id}");
        var lista = (await cliente.GetFromJsonAsync<List<ProductoRespuesta>>("/api/productos", Json))!;
        Assert.Equal(HttpStatusCode.NoContent, baja.StatusCode);
        Assert.DoesNotContain(lista, p => p.Id == creado.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.DeleteAsync($"/api/productos/{creado.Id}")).StatusCode);
    }

    [Fact]
    public async Task ProductoInvalidoDa400()
    {
        var (_, _, cliente) = await Taller("ST6002");

        var sinNombre = await cliente.PostAsJsonAsync("/api/productos", new { nombre = " ", precio = 10 }, Json);
        var sinPrecio = await cliente.PostAsJsonAsync("/api/productos", new { nombre = "Filtro" }, Json);
        var precioNegativo = await cliente.PostAsJsonAsync("/api/productos", new { nombre = "Filtro", precio = -5 }, Json);

        Assert.All(new[] { sinNombre, sinPrecio, precioNegativo }, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task AislamientoDeProductos()
    {
        var (tallerA, _, clienteA) = await Taller("ST6003");
        var (tallerB, _, clienteB) = await Taller("SU6003");
        var deB = await CrearProducto(clienteB, "Producto de B", 100m, 5);
        var deA = await Leer<ProductoRespuesta>(await clienteA.PostAsJsonAsync("/api/productos",
            new { nombre = "Producto de A", precio = 50m, stockActual = 1, tallerId = tallerB }, Json));

        var listaA = (await clienteA.GetFromJsonAsync<List<ProductoRespuesta>>("/api/productos", Json))!;
        var editarAjeno = await clienteA.PutAsJsonAsync($"/api/productos/{deB.Id}", new { nombre = "Hackeado", precio = 1m }, Json);
        var borrarAjeno = await clienteA.DeleteAsync($"/api/productos/{deB.Id}");
        var venderAjeno = await clienteA.PostAsJsonAsync("/api/ventas", new { productoId = deB.Id, cantidad = 1 }, Json);

        Assert.Contains(listaA, p => p.Id == deA.Id);
        Assert.DoesNotContain(listaA, p => p.Id == deB.Id);
        Assert.Equal(HttpStatusCode.NotFound, editarAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, borrarAjeno.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, venderAjeno.StatusCode);
        await using var dbA = fabrica.CrearContexto(tallerA);
        Assert.Equal(tallerA, (await dbA.Productos.SingleAsync(p => p.Id == deA.Id)).TallerId);
        await using var dbB = fabrica.CrearContexto(tallerB);
        var intacto = await dbB.Productos.SingleAsync(p => p.Id == deB.Id);
        Assert.Equal(("Producto de B", 5, true), (intacto.Nombre, intacto.StockActual, intacto.Activo));
    }

    // ================= Fase 6: ventas =================

    [Fact]
    public async Task VenderDescuentaStockYCongelaElPrecio()
    {
        var (_, _, cliente) = await Taller("VE6001");
        var producto = await CrearProducto(cliente, "Lámpara H4", 5000m, 10);

        var r = await cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 3 }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var venta = await Leer<VentaRespuesta>(r);
        Assert.Equal((3, 5000m, 15000m, 7, false), (venta.Cantidad, venta.PrecioUnitario, venta.Total, venta.StockRestante, venta.SinStockSuficiente));

        // Sube el precio del producto: la venta ya registrada no cambia.
        await cliente.PutAsJsonAsync($"/api/productos/{producto.Id}", new { nombre = "Lámpara H4", precio = 9999m }, Json);
        var historial = (await cliente.GetFromJsonAsync<List<VentaHistorial>>("/api/historial/ventas", Json))!;
        var enHistorial = historial.Single(v => v.Id == venta.Id);
        Assert.Equal(5000m, enHistorial.PrecioUnitario);
        Assert.Equal("Lámpara H4", enHistorial.Producto);
    }

    [Fact]
    public async Task VenderConPrecioEditadoAMano()
    {
        var (_, _, cliente) = await Taller("VE6002");
        var producto = await CrearProducto(cliente, "Escobillas", 8000m, 2);

        var venta = await Leer<VentaRespuesta>(await cliente.PostAsJsonAsync("/api/ventas",
            new { productoId = producto.Id, cantidad = 1, precioUnitario = 7000m }, Json));

        Assert.Equal(7000m, venta.PrecioUnitario);
    }

    [Fact]
    public async Task VenderSinStockNoSeBloqueaYDejaElStockNegativoConAviso()
    {
        var (_, _, cliente) = await Taller("VE6003");
        var producto = await CrearProducto(cliente, "Bujía", 3000m, 1);

        var primera = await Leer<VentaRespuesta>(await cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 1 }, Json));
        var segunda = await cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 2 }, Json);

        Assert.False(primera.SinStockSuficiente);
        Assert.Equal(0, primera.StockRestante);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode); // RF-28: nunca un error
        var venta = await Leer<VentaRespuesta>(segunda);
        Assert.True(venta.SinStockSuficiente);
        Assert.Equal(-2, venta.StockRestante);
        var lista = (await cliente.GetFromJsonAsync<List<ProductoRespuesta>>("/api/productos", Json))!;
        Assert.Equal(-2, lista.Single(p => p.Id == producto.Id).StockActual);
    }

    [Fact]
    public async Task VentasSimultaneasDescuentanTodasSinPisarse()
    {
        var (_, _, cliente) = await Taller("VE6004");
        var producto = await CrearProducto(cliente, "Tornillo", 10m, 100);

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 2 }, Json)));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var lista = (await cliente.GetFromJsonAsync<List<ProductoRespuesta>>("/api/productos", Json))!;
        Assert.Equal(60, lista.Single(p => p.Id == producto.Id).StockActual); // 100 - 20×2, descuento atómico
    }

    [Fact]
    public async Task VentaInvalidaDa400()
    {
        var (_, _, cliente) = await Taller("VE6005");
        var producto = await CrearProducto(cliente, "Fusible", 500m, 5);

        var inexistente = await cliente.PostAsJsonAsync("/api/ventas", new { productoId = Guid.NewGuid(), cantidad = 1 }, Json);
        var cantidadCero = await cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 0 }, Json);
        var precioNegativo = await cliente.PostAsJsonAsync("/api/ventas", new { productoId = producto.Id, cantidad = 1, precioUnitario = -1 }, Json);

        Assert.All(new[] { inexistente, cantidadCero, precioNegativo }, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task ElHistorialDeVentasEsDeCadaTallerYConservaProductosDadosDeBaja()
    {
        var (tallerA, _, clienteA) = await Taller("VE6006");
        var (_, _, clienteB) = await Taller("VF6006");
        var deA = await CrearProducto(clienteA, "Producto A", 100m, 5);
        var deB = await CrearProducto(clienteB, "Producto B", 100m, 5);
        var ventaA = await Leer<VentaRespuesta>(await clienteA.PostAsJsonAsync("/api/ventas", new { productoId = deA.Id, cantidad = 1, tallerId = Guid.NewGuid() }, Json));
        var ventaB = await Leer<VentaRespuesta>(await clienteB.PostAsJsonAsync("/api/ventas", new { productoId = deB.Id, cantidad = 1 }, Json));
        await clienteA.DeleteAsync($"/api/productos/{deA.Id}"); // baja lógica

        var historialA = (await clienteA.GetFromJsonAsync<List<VentaHistorial>>("/api/historial/ventas", Json))!;

        Assert.Contains(historialA, v => v.Id == ventaA.Id && v.Producto == "Producto A");
        Assert.DoesNotContain(historialA, v => v.Id == ventaB.Id);
        await using var db = fabrica.CrearContexto(tallerA);
        Assert.Equal(tallerA, (await db.Ventas.SingleAsync(v => v.Id == ventaA.Id)).TallerId);
    }

    [Fact]
    public async Task SinTokenTodoDa401()
    {
        var c = fabrica.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/servicios", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PatchAsJsonAsync($"/api/servicios/{id}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/productos", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PutAsJsonAsync($"/api/productos/{id}", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.DeleteAsync($"/api/productos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/ventas", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/historial/ventas")).StatusCode);
    }

    // ---- regla pura de stock (RF-28), sin base de datos ----

    [Theory]
    [InlineData(5, 3, false)]
    [InlineData(3, 3, false)]
    [InlineData(2, 3, true)]
    [InlineData(0, 1, true)]
    [InlineData(-4, 1, true)]
    public void SinStockSuficiente(int stock, int cantidad, bool esperado) =>
        Assert.Equal(esperado, ReglasDeStock.SinStockSuficiente(stock, cantidad));
}
