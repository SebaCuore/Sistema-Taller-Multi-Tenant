using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data.Configuraciones;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Tests;

/// <summary>
/// Batería de aislamiento por taller_id, a nivel DbContext (todavía no hay endpoints de negocio;
/// la versión HTTP de estas pruebas entra con GET/POST /api/vehiculos en la Fase 2).
/// </summary>
[Collection("api")]
public class AislamientoTallerTests(SistemaTallerFactory fabrica)
{
    [Fact]
    public async Task ListarNoDevuelveDatosDeOtroTaller()
    {
        var (tallerA, vehiculoA) = await fabrica.SembrarTallerConVehiculo("AAA111");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("BBB222");

        await using var db = fabrica.CrearContexto(tallerA);
        var ids = await db.Vehiculos.Select(v => v.Id).ToListAsync();

        Assert.Contains(vehiculoA, ids);
        Assert.DoesNotContain(vehiculoB, ids);
    }

    [Fact]
    public async Task PedirPorIdUnRecursoDeOtroTallerNoLoEncuentra()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("AAA333");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("BBB444");

        await using var db = fabrica.CrearContexto(tallerA);

        Assert.Null(await db.Vehiculos.FirstOrDefaultAsync(v => v.Id == vehiculoB));
    }

    [Fact]
    public async Task FindAsyncRespetaElFiltro()
    {
        // Find/FindAsync es un camino distinto a las consultas LINQ: se comprueba aparte.
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("AAA334");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("BBB445");

        await using var db = fabrica.CrearContexto(tallerA);

        Assert.Null(await db.Vehiculos.FindAsync(vehiculoB));
    }

    [Fact]
    public async Task UnTallerIdAsignadoAManoSeIgnoraAlGuardar()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("AAA555");
        var (tallerB, _) = await fabrica.SembrarTallerConVehiculo("BBB666");
        var id = Guid.CreateVersion7();

        await using (var db = fabrica.CrearContexto(tallerA))
        {
            db.Vehiculos.Add(new Vehiculo
            {
                Id = id, Patente = "ZZ999ZZ", TallerId = tallerB, // intento de colarlo en el otro taller
                TipoVehiculoId = TipoVehiculoConfiguration.IdAuto, FechaAlta = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using var lectura = fabrica.CrearContexto(tallerA);
        var guardado = await lectura.Vehiculos.SingleAsync(v => v.Id == id);
        Assert.Equal(tallerA, guardado.TallerId);
    }

    [Fact]
    public async Task ElFiltroSeEvaluaPorContextoYNoQuedaFijadoAlPrimerTaller()
    {
        // Dos contextos con el mismo modelo cacheado de EF: cada uno debe ver solo lo suyo.
        var (tallerA, vehiculoA) = await fabrica.SembrarTallerConVehiculo("AAA777");
        var (tallerB, vehiculoB) = await fabrica.SembrarTallerConVehiculo("BBB888");

        await using var dbA = fabrica.CrearContexto(tallerA);
        await using var dbB = fabrica.CrearContexto(tallerB);

        var idsA = await dbA.Vehiculos.Select(v => v.Id).ToListAsync();
        var idsB = await dbB.Vehiculos.Select(v => v.Id).ToListAsync();

        Assert.Contains(vehiculoA, idsA);
        Assert.DoesNotContain(vehiculoB, idsA);
        Assert.Contains(vehiculoB, idsB);
        Assert.DoesNotContain(vehiculoA, idsB);
    }
}
