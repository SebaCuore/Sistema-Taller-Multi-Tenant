using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaTaller.Api.Data;

namespace SistemaTaller.Api.Features.Auth;

public record LoginRequest(string? Email, string? Password);
public record LoginRespuesta(string Token);

public static class AuthEndpoints
{
    public const string Emisor = "sistema-taller-api";
    public const string Audiencia = "sistema-taller-frontend";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Único endpoint sin RequireAuthorization.
        app.MapPost("/auth/login", async (LoginRequest body, TallerDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrEmpty(body.Password))
                return Results.Problem(statusCode: 401, title: "Usuario o contraseña incorrectos");

            var email = body.Email.Trim().ToLowerInvariant();

            // Única excepción legítima a IgnoreQueryFilters: todavía no hay taller_id conocido.
            var usuario = await db.Usuarios.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

            if (usuario is null || !BCrypt.Net.BCrypt.Verify(body.Password, usuario.PasswordHash))
                return Results.Problem(statusCode: 401, title: "Usuario o contraseña incorrectos");

            var claves = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var token = new JwtSecurityToken(
                issuer: Emisor,
                audience: Audiencia,
                claims:
                [
                    new Claim("sub", usuario.Id.ToString()),
                    new Claim("taller_id", usuario.TallerId.ToString()),
                    new Claim("rol", usuario.Rol.ToString()),
                ],
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(claves, SecurityAlgorithms.HmacSha256));

            return Results.Ok(new LoginRespuesta(new JwtSecurityTokenHandler().WriteToken(token)));
        });
    }
}
