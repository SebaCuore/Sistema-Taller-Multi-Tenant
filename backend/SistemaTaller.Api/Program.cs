using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data;
using System.Text.Json.Serialization;
using SistemaTaller.Api.Features.Auth;
using SistemaTaller.Api.Features.Historial;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Servicios;
using SistemaTaller.Api.Features.Stock;
using SistemaTaller.Api.Features.Ventas;
using SistemaTaller.Api.Features.Vehiculos;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta Jwt:Key (dotnet user-secrets o variable de entorno).");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key tiene que tener al menos 32 caracteres.");

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
builder.Services.AgregarLimiteDeLogin(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS solo hace falta cuando el frontend se sirve desde otro origen (Vite en desarrollo).
// En producción la propia API sirve el frontend (wwwroot) y FrontendOrigin puede quedar vacío.
var origenFrontend = builder.Configuration["FrontendOrigin"];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrWhiteSpace(origenFrontend))
            policy.WithOrigins(origenFrontend).AllowAnyHeader().AllowAnyMethod();
    }));

// Detrás del proxy del hosting, el esquema real (https) llega en X-Forwarded-Proto.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Modos de comando único: dotnet run -- crear-admin <email> <contraseña>  |  cambiar-password <email> <nueva>  |  migrar
if (args.Length > 0 && args[0] == "crear-admin")
{
    return await ComandosUsuarios.CrearAdminAsync(app, args);
}
if (args.Length > 0 && args[0] == "cambiar-password")
{
    return await ComandosUsuarios.CambiarPasswordAsync(app, args);
}
var soloMigrar = args.Length > 0 && args[0] == "migrar";
if (soloMigrar || app.Configuration.GetValue<bool>("MigrarAlIniciar"))
{
    // Aplica las migraciones pendientes (y el seed). En producción: MigrarAlIniciar=true o el comando "migrar".
    using var scope = app.Services.CreateScope();
    var opciones = scope.ServiceProvider.GetRequiredService<DbContextOptions<TallerDbContext>>();
    await using var db = new TallerDbContext(opciones, new TallerFijo(Guid.Empty));
    await db.Database.MigrateAsync();
    if (soloMigrar)
    {
        Console.WriteLine("Migraciones aplicadas.");
        return 0;
    }
}

app.UseForwardedHeaders(); // antes del límite de intentos: así la IP es la del cliente y no la del proxy
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection(); // con el proxy del hosting ya en https no redirige; protege si alguien entra por http
}
app.UseExceptionHandler();
// Frontend compilado (wwwroot), cuando existe: en producción la API y la web salen del mismo origen.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapVehiculoEndpoints();
app.MapServicioEndpoints();
app.MapOrdenEndpoints();
app.MapHistorialEndpoints();
app.MapProductoEndpoints();
app.MapVentaEndpoints();

// Rutas del frontend (/historial, /ordenes/…): devuelven index.html. Las de la API que no existen siguen dando 404.
app.MapFallback(async contexto =>
{
    var indice = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
    var esApi = contexto.Request.Path.StartsWithSegments("/api") || contexto.Request.Path.StartsWithSegments("/auth");
    if (esApi || !indice.Exists || !HttpMethods.IsGet(contexto.Request.Method))
    {
        contexto.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    contexto.Response.ContentType = "text/html; charset=utf-8";
    contexto.Response.Headers.CacheControl = "no-cache";
    await contexto.Response.SendFileAsync(indice);
});

app.Run();
return 0;

public partial class Program;
