using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaTaller.Api.Data;

namespace SistemaTaller.Api.Features.Auth;

public record LoginRequest(string? Email, string? Password);
public record LoginRespuesta(string Token);
public record CambiarPasswordRequest(string? PasswordActual, string? PasswordNueva);

public static class AuthEndpoints
{
    public const string Emisor = "sistema-taller-api";
    public const string Audiencia = "sistema-taller-frontend";
    public const string PoliticaLogin = "login";
    public const int DiasDeSesion = 30;
    public const int LargoMinimoPassword = 8;
    private const int BytesMaximosPassword = 72; // BCrypt ignora lo que pase de 72 bytes

    /// <summary>Límite de intentos de login por IP: ventana fija (10 por minuto por defecto), el resto de la API no se limita.</summary>
    public static void AgregarLimiteDeLogin(this IServiceCollection servicios, IConfiguration config)
    {
        var permitidos = config.GetValue("Login:IntentosPorVentana", 10);
        var ventana = TimeSpan.FromSeconds(config.GetValue("Login:VentanaSegundos", 60));
        servicios.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(PoliticaLogin, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitidos,
                    Window = ventana,
                    QueueLimit = 0,
                }));
            o.OnRejected = (contexto, _) =>
            {
                contexto.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Auth")
                    .LogWarning("Login limitado (429) para la IP {Ip}", contexto.HttpContext.Connection.RemoteIpAddress);
                return ValueTask.CompletedTask;
            };
        });
    }

    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Sin RequireAuthorization: todavía no hay sesión. Es lo único que se limita por IP.
        app.MapPost("/auth/login", async (LoginRequest body, HttpContext http, TallerDbContext db, IConfiguration config, ILoggerFactory logs) =>
        {
            var log = logs.CreateLogger("Auth");
            var ip = http.Connection.RemoteIpAddress;
            var email = (body.Email ?? "").Trim().ToLowerInvariant();

            if (email.Length == 0 || string.IsNullOrEmpty(body.Password))
            {
                log.LogWarning("Login fallido (datos incompletos) desde {Ip}", ip);
                return Results.Problem(statusCode: 401, title: "Usuario o contraseña incorrectos");
            }

            // Única excepción legítima a IgnoreQueryFilters: todavía no hay taller_id conocido.
            var usuario = await db.Usuarios.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

            if (usuario is null || !BCrypt.Net.BCrypt.Verify(body.Password, usuario.PasswordHash))
            {
                log.LogWarning("Login fallido para {Email} desde {Ip}", Limpiar(email), ip);
                return Results.Problem(statusCode: 401, title: "Usuario o contraseña incorrectos");
            }

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
                expires: DateTime.UtcNow.AddDays(DiasDeSesion),
                signingCredentials: new SigningCredentials(claves, SecurityAlgorithms.HmacSha256));

            return Results.Ok(new LoginRespuesta(new JwtSecurityTokenHandler().WriteToken(token)));
        }).RequireRateLimiting(PoliticaLogin);

        // El usuario sale del claim "sub", nunca del body. La contraseña actual incorrecta es 400 (no 401):
        // el frontend trata el 401 como sesión vencida y manda al login.
        app.MapPost("/auth/cambiar-password", async (CambiarPasswordRequest body, HttpContext http, TallerDbContext db) =>
        {
            if (!Guid.TryParse(http.User.FindFirst("sub")?.Value, out var usuarioId)) return Results.Unauthorized();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo);
            if (usuario is null) return Results.Unauthorized();

            var errores = new Dictionary<string, string[]>();
            if (string.IsNullOrEmpty(body.PasswordActual) || !BCrypt.Net.BCrypt.Verify(body.PasswordActual, usuario.PasswordHash))
                errores["passwordActual"] = ["La contraseña actual no es correcta."];
            var nueva = body.PasswordNueva ?? "";
            if (nueva.Length < LargoMinimoPassword)
                errores["passwordNueva"] = [$"La contraseña nueva tiene que tener al menos {LargoMinimoPassword} caracteres."];
            else if (Encoding.UTF8.GetByteCount(nueva) > BytesMaximosPassword)
                errores["passwordNueva"] = ["La contraseña nueva es demasiado larga."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nueva);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // Para el monitor externo del hosting. Sin autenticación y sin datos: solo si la API y la base responden.
        app.MapGet("/salud", async (TallerDbContext db) =>
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                return await db.Database.CanConnectAsync(limite.Token)
                    ? Results.StatusCode(StatusCodes.Status200OK)
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception)
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });
    }

    /// <summary>Saca saltos de línea y caracteres de control para que un email no pueda falsificar líneas del log.</summary>
    private static string Limpiar(string texto) => new(texto.Where(c => !char.IsControl(c)).Take(200).ToArray());
}
