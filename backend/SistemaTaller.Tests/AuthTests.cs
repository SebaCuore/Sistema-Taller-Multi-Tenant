using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Features.Auth;

namespace SistemaTaller.Tests;

[Collection("api")]
public class AuthTests(SistemaTallerFactory fabrica)
{
    [Fact]
    public async Task LoginCorrectoDevuelveTokenConClaimsDelTaller()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT001");
        await fabrica.SembrarUsuario(taller, "ok@ejemplo.com", "contraseña-1");
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/auth/login", new { email = "OK@ejemplo.com", password = "contraseña-1" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(cuerpo!.Token);
        Assert.Equal(taller.ToString(), jwt.Claims.Single(c => c.Type == "taller_id").Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "rol").Value);
    }

    [Theory]
    [InlineData("mal@ejemplo.com", "incorrecta")]
    [InlineData("no-existe@ejemplo.com", "contraseña-1")]
    [InlineData("", "")]
    public async Task LoginIncorrectoDa401ConMensajeGenerico(string email, string password)
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT002");
        await fabrica.SembrarUsuario(taller, $"mal-{Guid.NewGuid():N}@ejemplo.com", "contraseña-1");
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("Usuario o contraseña incorrectos", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LoginDevuelveTokenQueDuraTreintaDias()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT003");
        var email = $"dias-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(taller, email, "contraseña-1");

        var r = await fabrica.CreateClient().PostAsJsonAsync("/auth/login", new { email, password = "contraseña-1" });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken((await r.Content.ReadFromJsonAsync<LoginRespuesta>())!.Token);

        Assert.InRange((jwt.ValidTo - DateTime.UtcNow).TotalDays, 29.9, 30.1);
    }

    [Fact]
    public async Task OnceIntentosEnLaVentanaDan429YDespuesDeLaVentanaVuelveAFuncionar()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT004");
        var email = $"limite-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(taller, email, "contraseña-1");
        using var limitada = fabrica.WithWebHostBuilder(b =>
        {
            b.UseSetting("Login:IntentosPorVentana", "10");
            b.UseSetting("Login:VentanaSegundos", "3");
        });
        var cliente = limitada.CreateClient();

        for (var i = 1; i <= 10; i++)
        {
            var r = await cliente.PostAsJsonAsync("/auth/login", new { email, password = "incorrecta" });
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }
        var onceavo = await cliente.PostAsJsonAsync("/auth/login", new { email, password = "contraseña-1" });
        Assert.Equal(HttpStatusCode.TooManyRequests, onceavo.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(3.5));
        var despues = await cliente.PostAsJsonAsync("/auth/login", new { email, password = "contraseña-1" });
        Assert.Equal(HttpStatusCode.OK, despues.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordConLaActualCorrectaHabilitaLaNuevaYDescartaLaVieja()
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT005");
        var email = $"cambio-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(taller, email, "vieja-1234");
        var cliente = await ClienteLogueado(email, "vieja-1234");

        var r = await cliente.PostAsJsonAsync("/auth/cambiar-password", new { passwordActual = "vieja-1234", passwordNueva = "nueva-5678" });

        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        var anonimo = fabrica.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsJsonAsync("/auth/login", new { email, password = "vieja-1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.PostAsJsonAsync("/auth/login", new { email, password = "nueva-5678" })).StatusCode);
    }

    [Theory]
    [InlineData("incorrecta-1", "nueva-5678")]
    [InlineData("vieja-1234", "corta")]
    public async Task CambiarPasswordInvalidoDa400YNoCambiaNada(string actual, string nueva)
    {
        var (taller, _) = await fabrica.SembrarTallerConVehiculo("AUT006");
        var email = $"invalido-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(taller, email, "vieja-1234");
        var cliente = await ClienteLogueado(email, "vieja-1234");

        var r = await cliente.PostAsJsonAsync("/auth/cambiar-password", new { passwordActual = actual, passwordNueva = nueva });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.CreateClient().PostAsJsonAsync("/auth/login", new { email, password = "vieja-1234" })).StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordSinTokenDa401()
    {
        var r = await fabrica.CreateClient().PostAsJsonAsync("/auth/cambiar-password", new { passwordActual = "a", passwordNueva = "bbbbbbbb" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordAfectaSoloAlUsuarioDelTokenAunqueElBodyTraigaOtroEmail()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("AUT007");
        var (tallerB, _) = await fabrica.SembrarTallerConVehiculo("AUT008");
        var emailA = $"a-{Guid.NewGuid():N}@ejemplo.com";
        var emailB = $"b-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(tallerA, emailA, "clave-a-1234");
        await fabrica.SembrarUsuario(tallerB, emailB, "clave-b-1234");
        var clienteA = await ClienteLogueado(emailA, "clave-a-1234");

        await clienteA.PostAsJsonAsync("/auth/cambiar-password",
            new { email = emailB, tallerId = tallerB, passwordActual = "clave-a-1234", passwordNueva = "otra-clave-99" });

        Assert.Equal(HttpStatusCode.OK, (await fabrica.CreateClient().PostAsJsonAsync("/auth/login", new { email = emailB, password = "clave-b-1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fabrica.CreateClient().PostAsJsonAsync("/auth/login", new { email = emailA, password = "otra-clave-99" })).StatusCode);
    }

    [Fact]
    public async Task EmailRepetidoEnOtroTallerLoRechazaLaBase()
    {
        var (tallerA, _) = await fabrica.SembrarTallerConVehiculo("AUT009");
        var (tallerB, _) = await fabrica.SembrarTallerConVehiculo("AUT010");
        var email = $"unico-{Guid.NewGuid():N}@ejemplo.com";
        await fabrica.SembrarUsuario(tallerA, email, "clave-1234");

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => fabrica.SembrarUsuario(tallerB, email, "clave-1234"));
    }

    [Fact]
    public async Task SaludResponde200SinToken()
    {
        var r = await fabrica.CreateClient().GetAsync("/salud");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("", await r.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> ClienteLogueado(string email, string password)
    {
        var cliente = fabrica.CreateClient();
        var r = await cliente.PostAsJsonAsync("/auth/login", new { email, password });
        var token = (await r.Content.ReadFromJsonAsync<LoginRespuesta>())!.Token;
        cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    [Fact]
    public void TallerActualSinClaimFallaExplicito()
    {
        var accesor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        Assert.Throws<InvalidOperationException>(() => new TallerActual(accesor).TallerId);
    }
}
