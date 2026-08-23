# Backend

Define cómo se construye el backend: estructura de proyecto, entidades y configuración completa de Entity Framework Core, autenticación, manejo de errores y una guía paso a paso para arrancar. Complementa a `data_base.md` (qué existe) y `functional_requirements.md` (qué debe hacer); este documento es el "cómo" en código, con suficiente detalle como para empezar a programar sin tener que tomar decisiones de diseño sobre la marcha.

## Stack

| Pieza | Elección | Por qué |
|---|---|---|
| Runtime | **.NET 10** (LTS) | Versión con soporte largo vigente. Ver `architecture.md` para el porqué frente a Node.js. |
| Framework web | **ASP.NET Core**, con **Minimal APIs** organizadas en `MapGroup` por área funcional | Menos ceremonia que Controllers para el tamaño de este proyecto. |
| ORM | **Entity Framework Core 10** | Migraciones, LINQ tipado, y los *global query filters* que resuelven el aislamiento multi-taller en la capa de acceso a datos. |
| Base de datos | **PostgreSQL**, vía `Npgsql.EntityFrameworkCore.PostgreSQL` | Gratis, código abierto, soportado de fábrica por cualquier PaaS. |
| Autenticación | **JWT Bearer**, hecho a mano sobre la tabla `Usuario` — **no** ASP.NET Core Identity completo | Identity trae su propio modelo de usuario pensado para registro público/2FA — más de lo que este MVP necesita, y no coincide con el `Usuario` ya definido. |
| Hash de contraseña | `BCrypt.Net-Next` | Cumple RF-02 sin escribir hashing propio. |
| Organización del proyecto | **Un solo proyecto, por función** (no Clean Architecture) | Ver "Estructura de proyecto" abajo — decisión confirmada explícitamente para este proyecto. |

## Paquetes NuGet

Todo dentro de un único proyecto (`SistemaTaller.Api.csproj`):

```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.AspNetCore.Authentication.JwtBearer
System.IdentityModel.Tokens.Jwt
BCrypt.Net-Next
```

Más la herramienta global (una sola vez por máquina, no es un paquete del proyecto):

```
dotnet tool install --global dotnet-ef
```

## Cómo arrancar, paso a paso

```bash
# 0. Levantar Postgres (desde la raíz de "Sistema Taller", NO desde backend/) usando el
# docker-compose.yml del proyecto — ver la guía de Docker adjunta si es la primera vez.
docker compose up -d

# 1. Crear el proyecto (plantilla "web" = vacía, minimal API desde cero, sin controllers)
dotnet new web -n SistemaTaller.Api -o backend/SistemaTaller.Api
cd backend/SistemaTaller.Api

# 2. Agregar los paquetes
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package BCrypt.Net-Next

# 3. Secretos de desarrollo (nunca en appsettings.json) — usuario, contraseña y nombre de
# base deben coincidir exactamente con los del docker-compose.yml
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=sistema_taller;Username=postgres;Password=devpassword"
dotnet user-secrets set "Jwt:Key" "una-clave-aleatoria-de-al-menos-32-caracteres-larga"

# 4. Con las entidades, el DbContext y las configuraciones ya escritas (ver abajo):
dotnet ef migrations add InicialConSeed
dotnet ef database update

# 5. Crear el primer usuario administrador (una sola vez, ver sección "Datos iniciales")
dotnet run -- crear-admin admin@tallersanmartin.com "contraseña-temporal-cambiar"

# 6. Correr la API
dotnet run
```

A partir de acá, cada entidad/feature nueva sigue siempre el mismo círculo: agregar/editar la clase en `Data/Entidades`, su configuración en `Data/Configuraciones`, `dotnet ef migrations add NombreDescriptivo`, `dotnet ef database update`, y recién ahí el endpoint en `Features/`.

## Estructura de proyecto

Un solo proyecto Web API, organizado **por función** (vertical, todo lo de "Vehículos" junto) y no por capa técnica horizontal — para un dominio de este tamaño, separar por capa técnica esparce cada feature en varias carpetas sin necesidad. (Se evaluó Clean Architecture en varios proyectos separados y se descartó a propósito para este MVP — ver "Qué evitar a propósito" más abajo.)

```
backend/
  SistemaTaller.Api/
    Program.cs
    appsettings.json
    Data/
      TallerDbContext.cs
      Entidades/                  → clases POCO: Taller, Usuario, Vehiculo, Servicio, OrdenTrabajo...
      Configuraciones/            → una clase IEntityTypeConfiguration<T> por entidad
      Migrations/                 → generadas por EF Core, no se editan a mano
    Features/
      Auth/
        AuthEndpoints.cs
      Vehiculos/
        VehiculoEndpoints.cs
        VehiculoDtos.cs
      Servicios/
      OrdenesTrabajo/
      Stock/
      Ventas/
    Common/
      IPerteneceATaller.cs
      ITallerActual.cs
      TallerActual.cs
      GlobalExceptionHandler.cs
    SistemaTaller.Api.csproj
  SistemaTaller.Tests/          → cuando arranque testing de integración
```

Regla simple para no dudar dónde va un archivo nuevo: si es de dominio/base de datos, va a `Data/`; si es una operación que el frontend puede llamar, va a `Features/<Área>/`; si es infraestructura compartida por todo (el taller actual, autenticación, errores), va a `Common/`.

## Convenciones de entidades

- Nombres de clases y propiedades **en español, PascalCase**, siguiendo la convención de dominio ya establecida en `CLAUDE.md` (`Vehiculo`, `OrdenTrabajo`, `TallerId`).
- Las entidades son **POCOs limpios**, sin atributos de Data Annotations ni nada específico de EF Core — toda esa configuración vive en `Data/Configuraciones/`.
- **IDs**: `Guid`, generado en el cliente con `Guid.CreateVersion7()` (disponible desde .NET 9) — a diferencia de un `Guid` random clásico, la versión 7 es ordenable en el tiempo y evita fragmentar el índice en tablas que crecen todo el tiempo (`OrdenTrabajo`, `Venta`). Excepción: los datos sembrados con `HasData` (ver "Datos iniciales") necesitan `Guid` fijos y conocidos de antemano, no se puede usar `CreateVersion7()` ahí.
- **Montos**: siempre `decimal`, nunca `float`/`double`. En la configuración: `.HasColumnType("numeric(12,2)")`.
- **Fechas**: siempre en UTC (`DateTime.UtcNow` al crear), se convierten a hora de Argentina recién en el frontend. Ojo: Npgsql exige que un `DateTime` mapeado a `timestamptz` tenga `Kind = Utc`, si no tira una excepción en tiempo de ejecución.
- **Estados y categorías**: `enum` de C# mapeado a texto con `.HasConversion<string>()`, para que en la base quede legible (`'en_proceso'`, no un número).
- **Campos `activo`**: `Servicio` y `TipoVehiculo` nunca se borran — no exponer `DELETE`, solo `PATCH` que cambia `Activo`.
- **`DeleteBehavior`**: `Restrict` por defecto en todo lo que cuelga de `Taller`, `Vehiculo`, `Servicio`, `Producto`. Única excepción real: `OrdenTrabajo → OrdenTrabajoServicio` puede ser `Cascade`, aunque en la práctica una orden nunca se borra (es historial).
- **Índices** a crear a mano: `Vehiculo` con índice compuesto `(TallerId, Patente)` (RF-06, la consulta más frecuente del sistema); `Usuario` con índice único compuesto `(TallerId, Email)` (el email es único *dentro de un taller*, no global).

## Entidades completas

Traducción 1:1 de `data_base.md`. `TipoVehiculo` es la única tabla de negocio que **no** implementa `IPerteneceATaller` — es catálogo global (Auto/Moto), no tiene `taller_id` en el modelo lógico.

```csharp
// Common/IPerteneceATaller.cs
public interface IPerteneceATaller
{
    Guid TallerId { get; set; }
}

// Data/Entidades/Enums.cs
public enum RolUsuario { Admin }
public enum CategoriaServicio { Auto, Moto, Ambos }
public enum EstadoOrden { Ingresado, EnProceso, ListoParaRetirar, Entregado }
public enum Posicion { Delantero, Trasero }
public enum Lado { Izquierda, Derecha }

// Data/Entidades/Taller.cs
public class Taller
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}

// Data/Entidades/Usuario.cs
public class Usuario : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public RolUsuario Rol { get; set; } = RolUsuario.Admin;
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}

// Data/Entidades/TipoVehiculo.cs — catálogo GLOBAL, no lleva TallerId
public class TipoVehiculo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = ""; // "Auto", "Moto"
    public bool Activo { get; set; } = true;
}

// Data/Entidades/Vehiculo.cs
public class Vehiculo : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Patente { get; set; } = "";
    public Guid TipoVehiculoId { get; set; }
    public TipoVehiculo? TipoVehiculo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Anio { get; set; }
    public string? ContactoNombre { get; set; }
    public string? ContactoTelefono { get; set; }
    public DateTime FechaAlta { get; set; }
}

// Data/Entidades/Servicio.cs
public class Servicio : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Nombre { get; set; } = "";
    public CategoriaServicio CategoriaVehiculo { get; set; }
    public bool AplicaPosicion { get; set; }
    public bool AplicaLado { get; set; }
    public decimal? PrecioReferencia { get; set; }
    public bool Activo { get; set; } = true;
}

// Data/Entidades/OrdenTrabajo.cs
public class OrdenTrabajo : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid VehiculoId { get; set; }
    public Guid UsuarioId { get; set; }
    public EstadoOrden Estado { get; set; } = EstadoOrden.Ingresado;
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? Notas { get; set; }

    public List<OrdenTrabajoServicio> Servicios { get; set; } = [];
}

// Data/Entidades/OrdenTrabajoServicio.cs — lleva TallerId propio (redundante con
// OrdenTrabajo.TallerId) para que el filtro global la cubra sin depender de un join.
// Ver data_base.md.
public class OrdenTrabajoServicio : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid OrdenTrabajoId { get; set; }
    public Guid ServicioId { get; set; }
    public Posicion? Posicion { get; set; }
    public Lado? Lado { get; set; }
    public decimal PrecioFinal { get; set; }
    public string? Notas { get; set; }
}

// Data/Entidades/Producto.cs
public class Producto : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
    public int StockActual { get; set; }
}

// Data/Entidades/Venta.cs
public class Venta : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public DateTime Fecha { get; set; }
}
```

## El filtro por `taller_id` (la parte que no se puede olvidar)

Implementación concreta del requisito no negociable de `architecture.md`: *"ninguna consulta puede ejecutarse sin filtrar por el `taller_id` del usuario autenticado, impuesto en la capa de acceso a datos, no endpoint por endpoint."*

**1. Servicio que expone el taller del usuario autenticado**, leído del JWT (nunca de un parámetro que mande el cliente):

```csharp
// Common/ITallerActual.cs
public interface ITallerActual
{
    Guid TallerId { get; }
}

// Common/TallerActual.cs
public sealed class TallerActual : ITallerActual
{
    public Guid TallerId { get; }

    public TallerActual(IHttpContextAccessor accessor)
    {
        var claim = accessor.HttpContext?.User.FindFirst("taller_id")
            ?? throw new InvalidOperationException("El usuario autenticado no tiene taller_id en el token.");
        TallerId = Guid.Parse(claim.Value);
    }
}
```

Se registra como `Scoped` — mismo ciclo de vida que el `DbContext`, así que encajan sin fricción.

**2. El `DbContext` aplica el filtro automáticamente a toda entidad que implemente `IPerteneceATaller`**, con un bucle sobre el modelo — así, si mañana se agrega una entidad nueva que implementa la interfaz, ya queda filtrada sin ningún paso manual que alguien se pueda olvidar. También asigna `TallerId` solo al guardar, para no depender de que el DTO de entrada lo traiga (nunca debería traerlo):

```csharp
// Data/TallerDbContext.cs
public class TallerDbContext : DbContext
{
    private readonly ITallerActual _tallerActual;

    public TallerDbContext(DbContextOptions<TallerDbContext> options, ITallerActual tallerActual)
        : base(options)
    {
        _tallerActual = tallerActual;
    }

    public DbSet<Taller> Talleres => Set<Taller>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TipoVehiculo> TiposVehiculo => Set<TipoVehiculo>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();
    public DbSet<OrdenTrabajoServicio> OrdenesTrabajoServicio => Set<OrdenTrabajoServicio>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TallerDbContext).Assembly);

        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IPerteneceATaller).IsAssignableFrom(entidad.ClrType)) continue;

            // Arma, por reflexión, el equivalente a: e => e.TallerId == _tallerActual.TallerId
            var parametro = Expression.Parameter(entidad.ClrType, "e");
            var propiedad = Expression.Property(parametro, nameof(IPerteneceATaller.TallerId));
            var valorActual = Expression.Constant(_tallerActual.TallerId);
            var comparacion = Expression.Equal(propiedad, valorActual);
            var lambda = Expression.Lambda(comparacion, parametro);

            // "FiltroTaller": filtro con nombre (EF Core 10) — deja lugar para sumar
            // otro filtro (ej. soft-delete) sobre la misma entidad sin pisar este.
            modelBuilder.Entity(entidad.ClrType).HasQueryFilter("FiltroTaller", lambda);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entrada in ChangeTracker.Entries<IPerteneceATaller>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.TallerId = _tallerActual.TallerId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

**Cuándo este filtro NO alcanza:** en cualquier consulta con SQL crudo (`FromSqlRaw`) y al usar `IgnoreQueryFilters()` — este último solo debería aparecer en un eventual panel de super-administrador cross-taller futuro, nunca en un endpoint normal del MVP.

## Configuraciones de EF Core (`Data/Configuraciones/`)

Una clase por entidad. Ejemplos representativos — el resto sigue el mismo patrón:

```csharp
public class TallerConfiguration : IEntityTypeConfiguration<Taller>
{
    public void Configure(EntityTypeBuilder<Taller> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.TallerId, x.Email }).IsUnique();
    }
}

public class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.Property(x => x.Patente).HasMaxLength(10).IsRequired();
        b.HasIndex(x => new { x.TallerId, x.Patente });
        b.HasOne(x => x.TipoVehiculo).WithMany()
            .HasForeignKey(x => x.TipoVehiculoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.CategoriaVehiculo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PrecioReferencia).HasColumnType("numeric(12,2)");
    }
}

public class OrdenTrabajoConfiguration : IEntityTypeConfiguration<OrdenTrabajo>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajo> b)
    {
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(30);
        b.HasOne<Vehiculo>().WithMany()
            .HasForeignKey(x => x.VehiculoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Servicios).WithOne()
            .HasForeignKey(x => x.OrdenTrabajoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrdenTrabajoServicioConfiguration : IEntityTypeConfiguration<OrdenTrabajoServicio>
{
    public void Configure(EntityTypeBuilder<OrdenTrabajoServicio> b)
    {
        b.Property(x => x.Posicion).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Lado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PrecioFinal).HasColumnType("numeric(12,2)");
        b.HasOne<Servicio>().WithMany()
            .HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Precio).HasColumnType("numeric(12,2)");
    }
}

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> b)
    {
        b.Property(x => x.PrecioUnitario).HasColumnType("numeric(12,2)");
        b.HasOne<Producto>().WithMany()
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

## Migraciones

- Se generan, nunca se escriben a mano (salvo un ajuste puntual y comentado).
- Flujo: `dotnet ef migrations add NombreDescriptivo`, **revisar el archivo generado antes de aplicarlo** (EF Core no siempre adivina bien un rename o un cambio de tipo), después `dotnet ef database update`.
- Nombres de migración en español, verbo + qué cambia: `AgregarTablaVenta`, `AgregarIndiceVehiculoPatente`.

## Datos iniciales (seed)

Dos mecanismos distintos, a propósito:

**1. Catálogos y datos de referencia → `HasData` en la configuración**, aplicado por una migración. Esto es lo estándar para datos que no cambian por el usuario final (o que casi no cambian): `TipoVehiculo` (Auto/Moto), el `Taller` único de hoy, y la lista por defecto de `Servicio` sugerida en `data_base.md`. `HasData` necesita `Guid` fijos, conocidos de antemano (no se puede usar `Guid.CreateVersion7()` acá):

```csharp
public class TipoVehiculoConfiguration : IEntityTypeConfiguration<TipoVehiculo>
{
    public static readonly Guid IdAuto = Guid.Parse("f8184fbd-3989-40c0-973a-0cc5bab89dab");
    public static readonly Guid IdMoto = Guid.Parse("6dece58d-f153-4c45-94cf-7c8b138a4879");

    public void Configure(EntityTypeBuilder<TipoVehiculo> b)
    {
        b.HasData(
            new TipoVehiculo { Id = IdAuto, Nombre = "Auto", Activo = true },
            new TipoVehiculo { Id = IdMoto, Nombre = "Moto", Activo = true }
        );
    }
}
```

Mismo patrón para sembrar el `Taller` ("San Martín", el mismo nombre que ya usa `design/prototipo.html`) y el catálogo de `Servicio` (Alineación, Balanceo, Reparación de neumático, Cambio de neumáticos, Rotación, Service de moto, Mecánica general — con los `aplica_posicion`/`aplica_lado` de `data_base.md`). **Los precios sembrados son los del prototipo, a confirmar con el dueño real del taller antes de ir a producción.**

**2. El primer usuario administrador → nunca por `HasData`.** Sembrar un hash de contraseña fijo dentro de una migración significa tener una contraseña conocida versionada en el repositorio — mal, incluso si "después se cambia". En cambio, un modo de comando único en el propio `Program.cs`, pensado para correrse una sola vez después de desplegar:

```csharp
// al principio de Program.cs, antes de levantar el servidor
if (args.Length > 0 && args[0] == "crear-admin")
{
    using var scope = ...; // build temporal del host solo para esto, o un builder liviano aparte
    var db = scope.ServiceProvider.GetRequiredService<TallerDbContext>();
    var taller = await db.Talleres.FirstAsync();

    db.Usuarios.Add(new Usuario
    {
        Id = Guid.CreateVersion7(),
        TallerId = taller.Id,
        Nombre = "Administrador",
        Email = args[1],
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(args[2]),
        Rol = RolUsuario.Admin,
        FechaAlta = DateTime.UtcNow,
    });
    await db.SaveChangesAsync();
    return;
}
```

(El detalle exacto de construir ese scope temporal — un `WebApplicationBuilder` liviano solo para este modo — se termina de resolver al escribir el `Program.cs` real; la idea central, y lo que no debe cambiar, es que el hash se calcula en el momento con la contraseña real del dueño, nunca hardcodeado en una migración.)

## DTOs y validación

- Los endpoints **nunca devuelven las entidades de EF Core directamente** — evita ciclos de serialización en las relaciones de navegación y evita exponer columnas internas por accidente. Se usan `record` de C# como DTOs, uno por feature:

```csharp
public record VehiculoRespuesta(Guid Id, string Patente, string Tipo, string? Marca, string? Modelo, int? Anio, string? ContactoNombre, string? ContactoTelefono);
public record VehiculoAlta(string Patente, Guid TipoVehiculoId, string? Marca, string? Modelo, int? Anio, string? ContactoNombre, string? ContactoTelefono);
```

- Validación mínima con Data Annotations en los DTOs de entrada (`[Required]`, `[MaxLength]`) más una validación explícita al principio de cada handler. No hace falta FluentValidation para el tamaño actual del proyecto.

## Autenticación (RF-01 a RF-04)

**Registro de servicios**, en `Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = "sistema-taller-api",
            ValidateAudience = true,
            ValidAudience = "sistema-taller-frontend",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();
```

**Emisión del token** en `POST /auth/login` (el único endpoint sin `.RequireAuthorization()`):

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest body, TallerDbContext db, IConfiguration config) =>
        {
            var usuario = await db.Usuarios.IgnoreQueryFilters() // todavía no hay taller_id conocido: recién lo da el login
                .FirstOrDefaultAsync(u => u.Email == body.Email && u.Activo);

            if (usuario is null || !BCrypt.Net.BCrypt.Verify(body.Password, usuario.PasswordHash))
                return Results.Problem(statusCode: 401, title: "Usuario o contraseña incorrectos");

            var claves = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credenciales = new SigningCredentials(claves, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "sistema-taller-api",
                audience: "sistema-taller-frontend",
                claims:
                [
                    new Claim("sub", usuario.Id.ToString()),
                    new Claim("taller_id", usuario.TallerId.ToString()),
                    new Claim("rol", usuario.Rol.ToString()),
                ],
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credenciales);

            return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        });
    }
}

public record LoginRequest(string Email, string Password);
```

**Nota importante sobre `IgnoreQueryFilters()` acá:** es una de las pocas excepciones legítimas mencionadas antes — en el momento del login todavía no se conoce el `taller_id` (es lo que el login tiene que averiguar), así que no hay forma de que el filtro global lo tuviera de todos modos. En todo el resto de la aplicación, después de autenticado, no debería volver a aparecer.

- El resto de los endpoints llevan `.RequireAuthorization()` en su `MapGroup`. RF-04 queda cumplido automáticamente por el filtro global de EF Core, siempre que el claim `taller_id` esté en el token — por eso es tan importante que el login lo incluya siempre.
- Aunque RF-03 dice que no hay roles granulares en el MVP, el claim `rol` se incluye desde ahora — el día que se sume `empleado` no hace falta tocar el formato del token.

## Reglas de dominio que hay que traducir con cuidado

- **Patente conocida, con confirmación explícita (RF-07):** el backend expone una consulta separada, de solo lectura (`GET /api/vehiculos/buscar-por-patente/{patente}`), que el frontend usa para *mostrar* el aviso. El alta o la actualización del contacto es siempre una acción explícita y posterior del usuario.
- **Stock negativo permitido (RF-28):** el endpoint de venta nunca devuelve un error (400/409) por falta de stock. Como mucho, la respuesta incluye un flag (`sinStockSuficiente: true`) para que el frontend muestre la advertencia suave.
- **`precio_final` y `precio_unitario` se copian una sola vez:** son columnas propias, no una relación calculada. Se copian al crear el registro y después viven independientes.
- **Descuento de stock atómico (RF-27):** usar `ExecuteUpdateAsync` (con lambdas normales desde EF Core 10) en vez de traer el `Producto` a memoria, restar en C# y guardar — evita una condición de carrera si dos ventas del mismo producto se registran casi al mismo tiempo:

```csharp
await db.Productos
    .Where(p => p.Id == venta.ProductoId)
    .ExecuteUpdateAsync(p => p.SetProperty(x => x.StockActual, x => x.StockActual - venta.Cantidad));
```

## Ejemplo de endpoint completo

```csharp
public static class VehiculoEndpoints
{
    public static void MapVehiculoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/vehiculos").RequireAuthorization();

        grupo.MapGet("/buscar-por-patente/{patente}", async (string patente, TallerDbContext db) =>
        {
            var vehiculo = await db.Vehiculos
                .Where(v => v.Patente == patente.ToUpper())
                .Select(v => new VehiculoRespuesta(v.Id, v.Patente, v.TipoVehiculo!.Nombre, v.Marca, v.Modelo, v.Anio, v.ContactoNombre, v.ContactoTelefono))
                .FirstOrDefaultAsync();

            return vehiculo is null ? Results.NotFound() : Results.Ok(vehiculo);
        });

        grupo.MapPost("/", async (VehiculoAlta alta, TallerDbContext db) =>
        {
            var vehiculo = new Vehiculo
            {
                Id = Guid.CreateVersion7(),
                Patente = alta.Patente.ToUpper(),
                TipoVehiculoId = alta.TipoVehiculoId,
                Marca = alta.Marca,
                Modelo = alta.Modelo,
                Anio = alta.Anio,
                ContactoNombre = alta.ContactoNombre,
                ContactoTelefono = alta.ContactoTelefono,
                FechaAlta = DateTime.UtcNow,
                // TallerId NO se setea acá — lo completa TallerDbContext.SaveChangesAsync
            };

            db.Vehiculos.Add(vehiculo);
            await db.SaveChangesAsync();

            return Results.Created($"/api/vehiculos/{vehiculo.Id}", vehiculo);
        });
    }
}
```

Registro de todos los grupos en `Program.cs`:

```csharp
app.MapAuthEndpoints();
app.MapVehiculoEndpoints();
app.MapServicioEndpoints();
app.MapOrdenTrabajoEndpoints();
app.MapStockEndpoints();
app.MapVentaEndpoints();
```

## Manejo global de errores

`IExceptionHandler` (disponible desde ASP.NET Core 8) para no repetir `try/catch` en cada endpoint, con respuesta en formato `ProblemDetails` (RFC 7807) estándar:

```csharp
// Common/GlobalExceptionHandler.cs
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Error no manejado: {Mensaje}", exception.Message);

        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocurrió un error inesperado",
        };

        context.Response.StatusCode = problema.Status.Value;
        await context.Response.WriteAsJsonAsync(problema, ct);
        return true;
    }
}
```

Registro en `Program.cs`:

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```

Para errores esperables dentro de un endpoint (no encontrado, datos inválidos), se sigue devolviendo el resultado explícito (`Results.NotFound()`, `Results.Problem(statusCode: 400, ...)`) — el handler global es la red de seguridad para lo que no se previó, no el mecanismo principal de validación.

## Configuración y secretos

- Cadena de conexión y clave de firma del JWT **nunca** en `appsettings.json` versionado. En desarrollo, `dotnet user-secrets`; en producción, variables de entorno del hosting.
- CORS: habilitar explícitamente solo el origen del frontend (`http://localhost:5173` en desarrollo con Vite, el dominio real en producción). Nunca `AllowAnyOrigin` en producción.

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["FrontendOrigin"]!)
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// ...
app.UseCors();
```

## Observabilidad mínima

- **Logging**: el `ILogger<T>` que ya viene inyectado por defecto alcanza para el MVP — nunca loguear `PasswordHash` ni el JWT completo.
- **Health check**, útil apenas se elija plataforma de hosting (muchas lo piden para saber si el contenedor está vivo):

```csharp
builder.Services.AddHealthChecks();
// ...
app.MapHealthChecks("/health");
```

## Qué evitar a propósito (para no sobre-diseñar)

Coherente con la filosofía de `description.md` ("escalable sin sobre-diseñar") y con la decisión explícita de no usar Clean Architecture para este proyecto:

- **Sin Clean Architecture de 4 proyectos** (Domain/Application/Infrastructure/Api) **ni MediatR** — evaluado y descartado a propósito: resuelve un problema (aislar el dominio de infraestructura reemplazable) que no aplica acá, porque no hay plan de cambiar de ORM ni de motor de base de datos.
- **Sin patrón Repository/UnitOfWork** envolviendo el `DbContext` — el `DbContext` ya es un Unit of Work, y cada `DbSet<T>` ya funciona como repositorio.
- **Sin ASP.NET Core Identity completo** — demasiado para un sistema de un solo rol sin registro público.
- **Sin tabla de permisos separada** — ya definido como decisión en `architecture.md`.
- **Sin AutoMapper** de entrada — mapear a mano mientras el número de campos por entidad se mantenga manejable.

## Testing

Estrategia completa, con la batería de tests de aislamiento por `taller_id` (prioridad máxima, gate de la Fase 1 de `plan_desarrollo.md`) en [`testing.md`](./testing.md). En resumen: **xUnit** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) + `Testcontainers.PostgreSql` para tests de integración contra una base Postgres real — nunca SQLite en memoria, no fuerza `Kind = Utc` ni las conversiones de enum a texto igual que Postgres, justo los dos puntos que este documento marca como propensos a romper en tiempo de ejecución.
