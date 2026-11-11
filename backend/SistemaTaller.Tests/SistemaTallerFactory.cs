using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Configuraciones;
using SistemaTaller.Api.Data.Entidades;
using Testcontainers.PostgreSql;

namespace SistemaTaller.Tests;

/// <summary>
/// API real contra un Postgres real y descartable. Si TESTS_CONNECTION_STRING está definida
/// se usa ese servidor (con una base nueva por corrida); si no, un Testcontainer (requiere Docker).
/// </summary>
public class SistemaTallerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ClaveJwt = "clave-solo-para-tests-0123456789-0123456789";

    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, Guid> _usuarios = new();
    private PostgreSqlContainer? _contenedor;
    private string? _servidorExterno;
    private string _nombreBase = "";
    private string _cadena = "";

    async Task IAsyncLifetime.InitializeAsync()
    {
        _servidorExterno = Environment.GetEnvironmentVariable("TESTS_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(_servidorExterno))
        {
            _contenedor = new PostgreSqlBuilder("postgres:18").Build();
            await _contenedor.StartAsync();
            _cadena = _contenedor.GetConnectionString();
        }
        else
        {
            _nombreBase = $"tests_{Guid.NewGuid():N}";
            await using var admin = new NpgsqlConnection(_servidorExterno);
            await admin.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_nombreBase}\"", admin);
            await cmd.ExecuteNonQueryAsync();
            _cadena = new NpgsqlConnectionStringBuilder(_servidorExterno) { Database = _nombreBase }.ConnectionString;
        }

        // Variables de entorno: Program las lee al construir el host, antes de cualquier ConfigureWebHost.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _cadena);
        Environment.SetEnvironmentVariable("Jwt__Key", ClaveJwt);

        await using var db = CrearContexto(Guid.Empty);
        await db.Database.MigrateAsync();
    }

    public TallerDbContext CrearContexto(Guid tallerId)
    {
        var opciones = new DbContextOptionsBuilder<TallerDbContext>().UseNpgsql(_cadena).Options;
        return new TallerDbContext(opciones, new TallerFijo(tallerId));
    }

    /// <summary>Crea un taller nuevo con un vehículo y devuelve sus ids.</summary>
    public async Task<(Guid TallerId, Guid VehiculoId)> SembrarTallerConVehiculo(string patente)
    {
        var tallerId = Guid.CreateVersion7();
        await using (var db = CrearContexto(Guid.Empty))
        {
            db.Talleres.Add(new Taller { Id = tallerId, Nombre = $"Taller {patente}", FechaAlta = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        // Cada taller tiene su propio catálogo: se copia el del taller sembrado por la migración.
        List<Servicio> modelo;
        await using (var origen = CrearContexto(TallerConfiguration.IdSanMartin))
            modelo = await origen.Servicios.AsNoTracking().ToListAsync();

        var vehiculoId = Guid.CreateVersion7();
        var usuarioId = Guid.CreateVersion7();
        _usuarios[tallerId] = usuarioId;
        await using (var db = CrearContexto(tallerId))
        {
            foreach (var s in modelo)
            {
                s.Id = Guid.CreateVersion7();
                db.Servicios.Add(s); // TallerId lo completa el contexto
            }
            // Usuario real del taller: las órdenes lo referencian (FK) desde el claim sub.
            db.Usuarios.Add(new Usuario
            {
                Id = usuarioId, Nombre = "Empleado", Email = "empleado@test", PasswordHash = "x", FechaAlta = DateTime.UtcNow,
            });
            db.Vehiculos.Add(new Vehiculo
            {
                Id = vehiculoId, Patente = patente, TipoVehiculoId = TipoVehiculoConfiguration.IdAuto,
                FechaAlta = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        return (tallerId, vehiculoId);
    }

    /// <summary>Cliente HTTP con un JWT válido del taller indicado (sin pasar por el login).</summary>
    public HttpClient CrearClienteAutenticadoComo(Guid tallerId)
    {
        var claves = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ClaveJwt));
        var token = new JwtSecurityToken(
            issuer: "sistema-taller-api", audience: "sistema-taller-frontend",
            claims: [new Claim("sub", (_usuarios.TryGetValue(tallerId, out var u) ? u : Guid.NewGuid()).ToString()), new Claim("taller_id", tallerId.ToString()), new Claim("rol", "Admin")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(claves, SecurityAlgorithms.HmacSha256));
        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return cliente;
    }

    public async Task SembrarUsuario(Guid tallerId, string email, string password)
    {
        await using var db = CrearContexto(tallerId);
        db.Usuarios.Add(new Usuario
        {
            Id = Guid.CreateVersion7(), Nombre = "Test", Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4), FechaAlta = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_contenedor is not null) await _contenedor.DisposeAsync();
        else if (!string.IsNullOrEmpty(_nombreBase))
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_servidorExterno);
            await admin.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_nombreBase}\" WITH (FORCE)", admin);
            await cmd.ExecuteNonQueryAsync();
        }
        await base.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public class ColeccionApi : ICollectionFixture<SistemaTallerFactory>;
