using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Common;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.Auth;

/// <summary>
/// Comandos de consola (<c>crear-admin</c>, <c>cambiar-password</c>). Corren sin usuario autenticado, así que
/// recorren los talleres con un contexto por taller en vez de usar IgnoreQueryFilters.
/// </summary>
public static class ComandosUsuarios
{
    public static async Task<int> CrearAdminAsync(WebApplication app, string[] args)
    {
        if (args.Length != 3 || string.IsNullOrWhiteSpace(args[1]) || args[2].Length < AuthEndpoints.LargoMinimoPassword)
        {
            Console.Error.WriteLine($"Uso: dotnet run -- crear-admin <email> <contraseña de al menos {AuthEndpoints.LargoMinimoPassword} caracteres>");
            return 1;
        }

        using var scope = app.Services.CreateScope();
        var opciones = scope.ServiceProvider.GetRequiredService<DbContextOptions<TallerDbContext>>();
        var email = args[1].Trim().ToLowerInvariant();

        // El email es único en todo el sistema: se busca en todos los talleres, no solo en el que se va a usar.
        if (await BuscarAsync(opciones, email) is not null)
        {
            Console.Error.WriteLine($"Ya existe un usuario con el email {email}.");
            return 1;
        }

        // Talleres no tiene filtro por taller; con esto se averigua el id del taller único de hoy.
        await using var consulta = new TallerDbContext(opciones, new TallerFijo(Guid.Empty));
        var taller = await consulta.Talleres.OrderBy(t => t.FechaAlta).FirstAsync();

        await using var db = new TallerDbContext(opciones, new TallerFijo(taller.Id));
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

    /// <summary>Recuperación de contraseña olvidada: la corre el dueño del proyecto desde la consola del hosting.</summary>
    public static async Task<int> CambiarPasswordAsync(WebApplication app, string[] args)
    {
        if (args.Length != 3 || string.IsNullOrWhiteSpace(args[1]) || args[2].Length < AuthEndpoints.LargoMinimoPassword)
        {
            Console.Error.WriteLine($"Uso: dotnet run -- cambiar-password <email> <contraseña nueva de al menos {AuthEndpoints.LargoMinimoPassword} caracteres>");
            return 1;
        }

        using var scope = app.Services.CreateScope();
        var opciones = scope.ServiceProvider.GetRequiredService<DbContextOptions<TallerDbContext>>();
        var email = args[1].Trim().ToLowerInvariant();

        var encontrado = await BuscarAsync(opciones, email);
        if (encontrado is null)
        {
            Console.Error.WriteLine($"No existe un usuario con el email {email}.");
            return 1;
        }

        await using var db = new TallerDbContext(opciones, new TallerFijo(encontrado.Value.TallerId));
        var usuario = await db.Usuarios.FirstAsync(u => u.Id == encontrado.Value.UsuarioId);
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(args[2]);
        await db.SaveChangesAsync();
        Console.WriteLine($"Contraseña de {email} actualizada.");
        return 0;
    }

    private static async Task<(Guid TallerId, Guid UsuarioId)?> BuscarAsync(DbContextOptions<TallerDbContext> opciones, string email)
    {
        await using var consulta = new TallerDbContext(opciones, new TallerFijo(Guid.Empty));
        foreach (var tallerId in await consulta.Talleres.Select(t => t.Id).ToListAsync())
        {
            await using var db = new TallerDbContext(opciones, new TallerFijo(tallerId));
            var id = await db.Usuarios.Where(u => u.Email == email).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
            if (id is not null) return (tallerId, id.Value);
        }
        return null;
    }
}
