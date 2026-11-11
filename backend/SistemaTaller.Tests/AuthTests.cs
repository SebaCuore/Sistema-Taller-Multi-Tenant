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
        await fabrica.SembrarUsuario(taller, "mal@ejemplo.com", "contraseña-1");
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Contains("Usuario o contraseña incorrectos", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public void TallerActualSinClaimFallaExplicito()
    {
        var accesor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        Assert.Throws<InvalidOperationException>(() => new TallerActual(accesor).TallerId);
    }
}
