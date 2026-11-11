using System.Net;
using System.Net.Http.Json;
using SistemaTaller.Api.Data.Configuraciones;
using SistemaTaller.Api.Features.Vehiculos;

namespace SistemaTaller.Tests;

/// <summary>Aislamiento por taller_id por HTTP + camino feliz de cada endpoint de vehículos.</summary>
[Collection("api")]
public class VehiculosTests(SistemaTallerFactory fabrica)
{
    private static object Alta(string patente, object? extra = null) => new
    {
        patente, tipoVehiculoId = TipoVehiculoConfiguration.IdAuto, marca = "Fiat", modelo = "Palio",
        anio = 2015, contactoNombre = "Juan Pérez", contactoTelefono = "11 5555-0000",
    };

    // ---- aislamiento ----

    [Fact]
    public async Task ListarNoDevuelveVehiculosDeOtroTaller()
    {
        var (tallerA, vehiculoA) = await fabrica.SembrarTallerConVehiculo("HA1001");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("HB1001");
        var cliente = fabrica.CrearClienteAutenticadoComo(tallerA);

        var lista = await cliente.GetFromJsonAsync<List<VehiculoRespuesta>>("/api/vehiculos");

        Assert.Contains(lista!, v => v.Id == vehiculoA);
        Assert.DoesNotContain(lista!, v => v.Id == vehiculoB);
    }

    [Fact]
    public async Task PedirPorIdUnVehiculoDeOtroTallerDa404()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("HA1002");
        var (_, vehiculoB) = await fabrica.SembrarTallerConVehiculo("HB1002");
        var cliente = fabrica.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.GetAsync($"/api/vehiculos/{vehiculoB}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task EditarUnVehiculoDeOtroTallerDa404YNoLoModifica()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("HA1003");
        var (tallerB, vehiculoB) = await fabrica.SembrarTallerConVehiculo("HB1003");
        var cliente = fabrica.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.PutAsJsonAsync($"/api/vehiculos/{vehiculoB}", Alta("HACKEADA"));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        await using var db = fabrica.CrearContexto(tallerB);
        Assert.Equal("HB1003", db.Vehiculos.Single(v => v.Id == vehiculoB).Patente);
    }

    [Fact]
    public async Task BuscarPorPatenteNoEncuentraLaDeOtroTaller()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("HA1004");
        await fabrica.SembrarTallerConVehiculo("HB1004");
        var cliente = fabrica.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.GetAsync("/api/vehiculos/buscar-por-patente/HB1004");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task AltaIgnoraUnTallerIdMandadoAManoEnElBody()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("HA1005");
        var (tallerB, _) = await fabrica.SembrarTallerConVehiculo("HB1005");
        var cliente = fabrica.CrearClienteAutenticadoComo(tallerA);

        var respuesta = await cliente.PostAsJsonAsync("/api/vehiculos", new
        {
            patente = "ZZ999ZZ", tipoVehiculoId = TipoVehiculoConfiguration.IdAuto, tallerId = tallerB,
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = await respuesta.Content.ReadFromJsonAsync<VehiculoRespuesta>();
        await using var db = fabrica.CrearContexto(tallerA);
        Assert.Equal(tallerA, db.Vehiculos.Single(v => v.Id == creado!.Id).TallerId);
    }

    [Fact]
    public async Task SinTokenTodosLosEndpointsDan401()
    {
        var cliente = fabrica.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/vehiculos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync($"/api/vehiculos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/vehiculos/buscar-por-patente/ABC123")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/vehiculos", Alta("X1"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PutAsJsonAsync($"/api/vehiculos/{id}", Alta("X1"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/tipos-vehiculo")).StatusCode);
    }

    // ---- camino feliz ----

    [Fact]
    public async Task AltaGuardaPatenteNormalizadaYSePuedeBuscar()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("HA2001");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);

        var alta = await cliente.PostAsJsonAsync("/api/vehiculos", Alta("af 402-qp"));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var creado = await alta.Content.ReadFromJsonAsync<VehiculoRespuesta>();
        Assert.Equal("AF402QP", creado!.Patente);
        Assert.Equal("Auto", creado.Tipo);

        var encontrado = await cliente.GetFromJsonAsync<VehiculoRespuesta>("/api/vehiculos/buscar-por-patente/AF%20402%20QP");
        Assert.Equal("Juan Pérez", encontrado!.ContactoNombre);
    }

    [Fact]
    public async Task BuscarPorPatenteDevuelveElRegistroMasReciente()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("HA2002");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        await cliente.PostAsJsonAsync("/api/vehiculos", new { patente = "REP123", tipoVehiculoId = TipoVehiculoConfiguration.IdAuto, contactoNombre = "Dueño viejo" });
        await cliente.PostAsJsonAsync("/api/vehiculos", new { patente = "REP123", tipoVehiculoId = TipoVehiculoConfiguration.IdAuto, contactoNombre = "Dueño nuevo" });

        var encontrado = await cliente.GetFromJsonAsync<VehiculoRespuesta>("/api/vehiculos/buscar-por-patente/REP123");

        Assert.Equal("Dueño nuevo", encontrado!.ContactoNombre);
    }

    [Fact]
    public async Task EditarActualizaLosDatos()
    {
        var (taller, vehiculo) = await fabrica.SembrarTallerConVehiculo("HA2003");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);

        var respuesta = await cliente.PutAsJsonAsync($"/api/vehiculos/{vehiculo}", Alta("HA2003"));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var editado = await cliente.GetFromJsonAsync<VehiculoRespuesta>($"/api/vehiculos/{vehiculo}");
        Assert.Equal("Palio", editado!.Modelo);
        Assert.Equal(2015, editado.Anio);
    }

    [Fact]
    public async Task TiposDeVehiculoDevuelveAutoYMoto()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("HA2004");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);

        var tipos = await cliente.GetFromJsonAsync<List<TipoVehiculoRespuesta>>("/api/tipos-vehiculo");

        Assert.Equal(["Auto", "Moto"], tipos!.Select(t => t.Nombre).ToArray());
    }

    // ---- errores esperables ----

    [Theory]
    [InlineData("", "tipo-valido", 2015)]
    [InlineData("ABC123", "tipo-invalido", 2015)]
    [InlineData("ABC123", "tipo-valido", 1800)]
    public async Task AltaInvalidaDa400(string patente, string tipo, int anio)
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("HA2005");
        var cliente = fabrica.CrearClienteAutenticadoComo(taller);
        var tipoId = tipo == "tipo-valido" ? TipoVehiculoConfiguration.IdAuto : Guid.NewGuid();

        var respuesta = await cliente.PostAsJsonAsync("/api/vehiculos", new { patente, tipoVehiculoId = tipoId, anio });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(" af 402 qp ", "AF402QP")]
    [InlineData("abc-123", "ABC123")]
    [InlineData(null, "")]
    public void NormalizarPatente(string? entrada, string esperado) =>
        Assert.Equal(esperado, Patentes.Normalizar(entrada));
}
