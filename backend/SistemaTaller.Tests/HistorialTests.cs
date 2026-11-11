using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Features.Historial;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Servicios;

namespace SistemaTaller.Tests;

[Collection("api")]
public class HistorialTests(SistemaTallerFactory fabrica)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Crea una orden con Balanceo (delantero izquierdo) + Rotación y la deja entregada o en curso.</summary>
    private async Task<(Guid Taller, HttpClient Cliente, OrdenDetalle Orden)> TallerConOrden(string patente, bool finalizar)
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo(patente);
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        var cat = (await cliente.GetFromJsonAsync<List<ServicioRespuesta>>("/api/servicios", Json))!;
        var r = await cliente.PostAsJsonAsync("/api/ordenes", new
        {
            vehiculoId = vehiculo,
            servicios = new object[]
            {
                new { servicioId = cat.Single(s => s.Nombre == "Balanceo").Id, posicion = "Delantero", lado = "Izquierda" },
                new { servicioId = cat.Single(s => s.Nombre == "Rotación").Id, precioFinal = 9000m },
            },
        }, Json);
        var orden = (await r.Content.ReadFromJsonAsync<OrdenDetalle>(Json))!;
        if (finalizar) await cliente.PostAsync($"/api/ordenes/{orden.Id}/finalizar", null);
        return (taller, cliente, orden);
    }

    private static Task<List<HistorialServicio>> Historial(HttpClient c, string consulta = "") =>
        c.GetFromJsonAsync<List<HistorialServicio>>($"/api/historial/servicios{consulta}", Json)!;

    [Fact]
    public async Task MuestraLaOrdenFinalizadaConFechaServiciosPosicionYPrecios()
    {
        var (_, cliente, orden) = await TallerConOrden("HI1001", finalizar: true);

        var lista = await Historial(cliente, "?patente=HI1001");

        var h = Assert.Single(lista);
        Assert.Equal(orden.Id, h.OrdenId);
        Assert.Equal(27000m, h.Total);
        Assert.Equal(2, h.Servicios.Count);
        var balanceo = h.Servicios.Single(s => s.Servicio == "Balanceo");
        Assert.Equal(Api.Data.Entidades.Posicion.Delantero, balanceo.Posicion);
        Assert.Equal(Api.Data.Entidades.Lado.Izquierda, balanceo.Lado);
        Assert.Equal(18000m, balanceo.PrecioFinal);
        Assert.True(h.FechaEntrega >= h.FechaIngreso);
    }

    [Fact]
    public async Task NoIncluyeLasOrdenesEnCurso()
    {
        var (_, cliente, enCurso) = await TallerConOrden("HI1002", finalizar: false);

        var lista = await Historial(cliente);

        Assert.DoesNotContain(lista, h => h.OrdenId == enCurso.Id);
    }

    [Fact]
    public async Task BuscarPorPatenteNormalizaYAceptaParteDeLaPatente()
    {
        var (_, cliente, orden) = await TallerConOrden("HI1003", finalizar: true);

        Assert.Contains(await Historial(cliente, "?patente=hi%201003"), h => h.OrdenId == orden.Id);
        Assert.Contains(await Historial(cliente, "?patente=I100"), h => h.OrdenId == orden.Id);
        Assert.Empty(await Historial(cliente, "?patente=NOEXISTE9"));
    }

    [Fact]
    public async Task ElHistorialNoMuestraOrdenesDeOtroTaller()
    {
        var (_, clienteA, ordenA) = await TallerConOrden("HI1004", finalizar: true);
        var (_, _, ordenB) = await TallerConOrden("HJ1004", finalizar: true);

        var todo = await Historial(clienteA);
        var buscandoLaAjena = await Historial(clienteA, "?patente=HJ1004");

        Assert.Contains(todo, h => h.OrdenId == ordenA.Id);
        Assert.DoesNotContain(todo, h => h.OrdenId == ordenB.Id);
        Assert.Empty(buscandoLaAjena);
    }

    [Fact]
    public async Task UnServicioDesactivadoSigueVisibleEnElHistorial()
    {
        var (taller, cliente, orden) = await TallerConOrden("HI1005", finalizar: true);
        await using (var db = fabrica.CrearContexto(taller))
            await db.Servicios.ExecuteUpdateAsync(s => s.SetProperty(x => x.Activo, false));

        var h = Assert.Single(await Historial(cliente, "?patente=HI1005"));

        Assert.Equal(orden.Id, h.OrdenId);
        Assert.Contains(h.Servicios, s => s.Servicio == "Balanceo");
    }

    [Fact]
    public async Task SinTokenDa401()
    {
        var r = await fabrica.CreateClient().GetAsync("/api/historial/servicios");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
