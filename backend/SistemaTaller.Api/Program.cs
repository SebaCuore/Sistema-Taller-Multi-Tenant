using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;
using System.Text.Json.Serialization;
using SistemaTaller.Api.Features.Auth;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Servicios;
using SistemaTaller.Api.Features.Vehiculos;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta Jwt:Key (dotnet user-secrets o variable de entorno).");

// Enums como texto también en JSON ("EnProceso", no 1).
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITallerActual, TallerActual>();
builder.Services.AddDbContext<TallerDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // conserva los nombres del token: "sub", "taller_id", "rol"
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = AuthEndpoints.Emisor,
            ValidateAudience = true,
            ValidAudience = AuthEndpoints.Audiencia,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["FrontendOrigin"]!)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Modo de comando único: dotnet run -- crear-admin <email> <contraseña>
if (args.Length > 0 && args[0] == "crear-admin")
{
    return await CrearAdmin.EjecutarAsync(app, args);
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapVehiculoEndpoints();
app.MapServicioEndpoints();
app.MapOrdenEndpoints();

app.Run();
return 0;

public partial class Program;

static class CrearAdmin
{
    public static async Task<int> EjecutarAsync(WebApplication app, string[] args)
    {
        if (args.Length != 3 || string.IsNullOrWhiteSpace(args[1]) || args[2].Length < 8)
        {
            Console.Error.WriteLine("Uso: dotnet run -- crear-admin <email> <contraseña de al menos 8 caracteres>");
            return 1;
        }

        using var scope = app.Services.CreateScope();
        var opciones = scope.ServiceProvider.GetRequiredService<DbContextOptions<TallerDbContext>>();

        // Talleres no tiene filtro por taller; con esto se averigua el id del taller único de hoy.
        await using var consulta = new TallerDbContext(opciones, new TallerFijo(Guid.Empty));
        var taller = await consulta.Talleres.OrderBy(t => t.FechaAlta).FirstAsync();

        await using var db = new TallerDbContext(opciones, new TallerFijo(taller.Id));
        var email = args[1].Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
        {
            Console.Error.WriteLine($"Ya existe un usuario con el email {email}.");
            return 1;
        }

        db.Usuarios.Add(new Usuario
        {
            Id = Guid.CreateVersion7(),
            Nombre = "Administrador",
            Email = email,
            // El hash se calcula acá con la contraseña real; nunca se siembra en una migración.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(args[2]),
            Rol = RolUsuario.Admin,
            FechaAlta = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        Console.WriteLine($"Administrador {email} creado en el taller \"{taller.Nombre}\".");
        return 0;
    }
}
