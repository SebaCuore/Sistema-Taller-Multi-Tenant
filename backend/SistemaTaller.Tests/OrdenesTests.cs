using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data.Entidades;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Servicios;

namespace SistemaTaller.Tests;

[Collection("api")]
public class OrdenesTests(SistemaTallerFactory fabrica)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static async Task<T> Leer<T>(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<T>(Json))!;

    private static async Task<List<ServicioRespuesta>> Catalogo(HttpClient c) =>
        (await c.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios?soloActivos=true", Json))!;

    private static ServicioRespuesta Por(List<ServicioRespuesta> cat, string nombre) => cat.Single(s => s.Nombre == nombre);

    /// <summary>Taller nuevo con un vehículo y una orden de un servicio (Rotación).</summary>
    private async Task<(Guid Taller, HttpClient Cliente, OrdenDetalle Orden)> TallerConOrden(string patente)
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo(patente);
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        var rotacion = Por(await Catalogo(cliente), "Rotación");
        var r = await cliente.PostAsJsonAsync("/api/ordenes",
            new { vehiculoId = vehiculo, servicios = new[] { new { servicioId = rotacion.Id } } }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (taller, cliente, await Leer<OrdenDetalle>(r));
    }

    // ---- aislamiento (OrdenTrabajo y OrdenTrabajoServicio) ----

    [Fact]
    public async Task ListarNoDevuelveOrdenesDeOtroTaller()
    {
        var (_, clienteA, ordenA) = await TallerConOrden("OA1001");
        var (_, _, ordenB) = await TallerConOrden("OB1001");

        var lista = await clienteA.GetFromJsonAsync<List<OrdenResumen>>("/api/ordenes", Json);

        Assert.Contains(lista!, o => o.Id == ordenA.Id);
        Assert.DoesNotContain(lista!, o => o.Id == ordenB.Id);
    }

    [Fact]
    public async Task PedirPorIdUnaOrdenDeOtroTallerDa404()
    {
        var (_, clienteA, _) = await TallerConOrden("OA1002");
        var (_, _, ordenB) = await TallerConOrden("OB1002");

        Assert.Equal(HttpStatusCode.NotFound, (await clienteA.GetAsync($"/api/ordenes/{ordenB.Id}")).StatusCode);
    }

    [Fact]
    public async Task OperarSobreUnaOrdenDeOtroTallerDa404YNoLaModifica()
    {
        var (_, clienteA, _) = await TallerConOrden("OA1003");
        var (tallerB, _, ordenB) = await TallerConOrden("OB1003");
        var itemB = ordenB.Servicios[0].Id;

        Assert.Equal(HttpStatusCode.NotFound, (await clienteA.DeleteAsync($"/api/ordenes/{ordenB.Id}/servicios/{itemB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteA.PatchAsJsonAsync($"/api/ordenes/{ordenB.Id}/estado", new { estado = "EnProceso" }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clienteA.PostAsync($"/api/ordenes/{ordenB.Id}/finalizar", null)).StatusCode);

        await using var db = fabrica.CrearContexto(tallerB);
        var orden = await db.OrdenesTrabajo.Include(o => o.Servicios).SingleAsync(o => o.Id == ordenB.Id);
        Assert.Equal(EstadoOrden.Ingresado, orden.Estado);
        Assert.Single(orden.Servicios);
    }

    [Fact]
    public async Task NoSePuedeCrearUnaOrdenSobreUnVehiculoOServicioDeOtroTaller()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("OA1004");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("OB1004");
        var clienteA = fabrica.CrearClienteAutenticadoComo(tallerA);
        var rotacion = Por(await Catalogo(clienteA), "Rotación");

        var r = await clienteA.PostAsJsonAsync("/api/ordenes",
            new { vehiculoId = vehiculoB, servicios = new[] { new { servicioId = rotacion.Id } } }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task AltaIgnoraTallerIdYUsuarioIdMandadosEnElBodyYLosItemsHeredanElTaller()
    {
        var (tallerA, vehiculoA) = await fabrica.SembrarTallerConVehiculo("OA1005");
        var (tallerB, _) = await fabrica.SembrarTallerConVehiculo("OB1005");
        var clienteA = fabrica.CrearClienteAutenticadoComo(tallerA);
        var rotacion = Por(await Catalogo(clienteA), "Rotación");

        var r = await clienteA.PostAsJsonAsync("/api/ordenes", new
        {
            vehiculoId = vehiculoA, tallerId = tallerB,
            servicios = new[] { new { servicioId = rotacion.Id, tallerId = tallerB } },
        }, Json);
        var orden = await Leer<OrdenDetalle>(r);

        await using var db = fabrica.CrearContexto(tallerA);
        Assert.Equal(tallerA, (await db.OrdenesTrabajo.SingleAsync(o => o.Id == orden.Id)).TallerId);
        // OrdenTrabajoServicio tiene taller_id propio y se completa solo (ver base_de_datos.md).
        Assert.Equal(tallerA, (await db.OrdenesTrabajoServicio.SingleAsync(s => s.Id == orden.Servicios[0].Id)).TallerId);
    }

    [Fact]
    public async Task SinTokenTodosLosEndpointsDeOrdenesYServiciosDan401()
    {
        var c = fabrica.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/ordenes")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync($"/api/ordenes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/ordenes", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync($"/api/ordenes/{id}/servicios", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.DeleteAsync($"/api/ordenes/{id}/servicios/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PatchAsJsonAsync($"/api/ordenes/{id}/estado", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync($"/api/ordenes/{id}/finalizar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PatchAsJsonAsync($"/api/ordenes/{id}/servicios/{id}", new { precioFinal = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/servicios")).StatusCode);
    }

    // ---- camino feliz y reglas de dominio ----

    [Fact]
    public async Task AltaConDosServiciosPrecargaPrecioDeReferenciaYPermitePrecioEditado()
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo("OA2001");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        var cat = await Catalogo(cliente);
        var balanceo = Por(cat, "Balanceo");
        var alineacion = Por(cat, "Alineación");

        var r = await cliente.PostAsJsonAsync("/api/ordenes", new
        {
            vehiculoId = vehiculo,
            servicios = new object[]
            {
                new { servicioId = balanceo.Id, posicion = "Delantero", lado = "Izquierda" }, // precio de referencia
                new { servicioId = alineacion.Id, precioFinal = 35000m },                      // precio editado a mano
            },
        }, Json);
        var orden = await Leer<OrdenDetalle>(r);

        Assert.Equal(EstadoOrden.Ingresado, orden.Estado);
        Assert.Equal(2, orden.Servicios.Count);
        Assert.Equal(18000m, orden.Servicios.Single(s => s.Servicio == "Balanceo").PrecioFinal);
        Assert.Equal(Posicion.Delantero, orden.Servicios.Single(s => s.Servicio == "Balanceo").Posicion);
        Assert.Equal(35000m, orden.Servicios.Single(s => s.Servicio == "Alineación").PrecioFinal);
        Assert.Equal(53000m, orden.Total);
    }

    [Fact]
    public async Task AltaSinServiciosOConPosicionEnServicioQueNoLaUsaDa400()
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo("OA2002");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        var rotacion = Por(await Catalogo(cliente), "Rotación"); // no aplica posición

        var sinServicios = await cliente.PostAsJsonAsync("/api/ordenes", new { vehiculoId = vehiculo, servicios = Array.Empty<object>() }, Json);
        var posicionIndebida = await cliente.PostAsJsonAsync("/api/ordenes",
            new { vehiculoId = vehiculo, servicios = new[] { new { servicioId = rotacion.Id, posicion = "Trasero" } } }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, sinServicios.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, posicionIndebida.StatusCode);
    }

    [Fact]
    public async Task AgregarYQuitarServiciosMientrasEstaEnCurso()
    {
        var (_, cliente, orden) = await TallerConOrden("OA2003");
        var mecanica = Por(await Catalogo(cliente), "Mecánica general");

        var agregado = await Leer<OrdenDetalle>(await cliente.PostAsJsonAsync($"/api/ordenes/{orden.Id}/servicios",
            new { servicioId = mecanica.Id, precioFinal = 50000m }, Json));
        Assert.Equal(2, agregado.Servicios.Count);
        Assert.Equal(50000m, agregado.Total);

        var quitado = await Leer<OrdenDetalle>(await cliente.DeleteAsync($"/api/ordenes/{orden.Id}/servicios/{orden.Servicios[0].Id}"));
        Assert.Single(quitado.Servicios);
        Assert.Equal("Mecánica general", quitado.Servicios[0].Servicio);
    }

    [Fact]
    public async Task SePuedeFinalizarDirectoDesdeIngresadoSinPasosIntermedios()
    {
        var (_, cliente, orden) = await TallerConOrden("OA2004");

        var r = await cliente.PostAsync($"/api/ordenes/{orden.Id}/finalizar", null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode); // RF-20: no es un error
        var cerrada = await Leer<OrdenDetalle>(r);
        Assert.Equal(EstadoOrden.Entregado, cerrada.Estado);
        Assert.NotNull(cerrada.FechaEntrega);
        var activas = await cliente.GetFromJsonAsync<List<OrdenResumen>>("/api/ordenes", Json);
        Assert.DoesNotContain(activas!, o => o.Id == orden.Id);
    }

    [Fact]
    public async Task CambiaDeEstadoYFiltraPorEstado()
    {
        var (_, cliente, orden) = await TallerConOrden("OA2005");

        var r = await cliente.PatchAsJsonAsync($"/api/ordenes/{orden.Id}/estado", new { estado = "ListoParaRetirar" }, Json);
        Assert.Equal(EstadoOrden.ListoParaRetirar, (await Leer<OrdenDetalle>(r)).Estado);

        var listos = await cliente.GetFromJsonAsync<List<OrdenResumen>>("/api/ordenes?estado=ListoParaRetirar", Json);
        var ingresados = await cliente.GetFromJsonAsync<List<OrdenResumen>>("/api/ordenes?estado=Ingresado", Json);
        Assert.Contains(listos!, o => o.Id == orden.Id);
        Assert.DoesNotContain(ingresados!, o => o.Id == orden.Id);
    }

    [Fact]
    public async Task UnaOrdenEntregadaNoSeModificaDa409()
    {
        var (_, cliente, orden) = await TallerConOrden("OA2006");
        var mecanica = Por(await Catalogo(cliente), "Mecánica general");
        await cliente.PostAsync($"/api/ordenes/{orden.Id}/finalizar", null);

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsync($"/api/ordenes/{orden.Id}/finalizar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync($"/api/ordenes/{orden.Id}/servicios", new { servicioId = mecanica.Id }, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/ordenes/{orden.Id}/servicios/{orden.Servicios[0].Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PatchAsJsonAsync($"/api/ordenes/{orden.Id}/estado", new { estado = "EnProceso" }, Json)).StatusCode);
    }

    [Fact]
    public async Task EntregarPorElEndpointDeEstadoDa400()
    {
        var (_, cliente, orden) = await TallerConOrden("OA2007");

        var r = await cliente.PatchAsJsonAsync($"/api/ordenes/{orden.Id}/estado", new { estado = "Entregado" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task ElPrecioQuedaCongeladoAunqueCambieElPrecioDeReferencia()
    {
        var (taller, cliente, orden) = await TallerConOrden("OA2008");
        var balanceo = Por(await Catalogo(cliente), "Balanceo");
        var con = await Leer<OrdenDetalle>(await cliente.PostAsJsonAsync($"/api/ordenes/{orden.Id}/servicios", new { servicioId = balanceo.Id }, Json));
        Assert.Equal(18000m, con.Servicios.Single(s => s.Servicio == "Balanceo").PrecioFinal);

        // Cambio el precio de referencia del servicio de este taller; la orden ya creada no se mueve.
        await using (var db = fabrica.CrearContexto(taller))
            await db.Servicios.Where(s => s.Id == balanceo.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PrecioReferencia, 99999m));
        var releida = await cliente.GetFromJsonAsync<OrdenDetalle>($"/api/ordenes/{orden.Id}", Json);

        Assert.Equal(18000m, releida!.Servicios.Single(s => s.Servicio == "Balanceo").PrecioFinal);
    }

    // ---- cambiar precio de un servicio ya cargado (RF-18) ----

    [Fact]
    public async Task CambiarElPrecioDeUnServicioYaCargadoRecalculaElTotal()
    {
        var (_, cliente, orden) = await TallerConOrden("OA3001");

        var r = await cliente.PatchAsJsonAsync($"/api/ordenes/{orden.Id}/servicios/{orden.Servicios[0].Id}", new { precioFinal = 12500.5m }, Json);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var actualizada = await Leer<OrdenDetalle>(r);
        Assert.Equal(12500.5m, actualizada.Servicios[0].PrecioFinal);
        Assert.Equal(12500.5m, actualizada.Total);
    }

    [Fact]
    public async Task CambiarPrecioEnUnaOrdenEntregadaDa409()
    {
        var (_, cliente, orden) = await TallerConOrden("OA3002");
        await cliente.PostAsync($"/api/ordenes/{orden.Id}/finalizar", null);

        var r = await cliente.PatchAsJsonAsync($"/api/ordenes/{orden.Id}/servicios/{orden.Servicios[0].Id}", new { precioFinal = 1m }, Json);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("null")]
    public async Task CambiarPrecioInvalidoDa400(string precio)
    {
        var (_, cliente, orden) = await TallerConOrden("OA3003");
        var cuerpo = new StringContent($"{{\"precioFinal\": {precio}}}", System.Text.Encoding.UTF8, "application/json");

        var r = await cliente.PatchAsync($"/api/ordenes/{orden.Id}/servicios/{orden.Servicios[0].Id}", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task CambiarPrecioDeUnServicioDeOtroTallerDa404YNoLoModifica()
    {
        var (_, clienteA, _) = await TallerConOrden("OA3004");
        var (tallerB, _, ordenB) = await TallerConOrden("OB3004");

        var r = await clienteA.PatchAsJsonAsync($"/api/ordenes/{ordenB.Id}/servicios/{ordenB.Servicios[0].Id}", new { precioFinal = 1m }, Json);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        await using var db = fabrica.CrearContexto(tallerB);
        Assert.Equal(ordenB.Servicios[0].PrecioFinal, (await db.OrdenesTrabajoServicio.SingleAsync(s => s.Id == ordenB.Servicios[0].Id)).PrecioFinal);
    }
}
