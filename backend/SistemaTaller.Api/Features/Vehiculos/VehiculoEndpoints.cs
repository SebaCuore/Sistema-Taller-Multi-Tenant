using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.Vehiculos;

public record VehiculoRespuesta(
    Guid Id, string Patente, Guid TipoVehiculoId, string Tipo, string? Marca, string? Modelo,
    int? Anio, string? ContactoNombre, string? ContactoTelefono);

// Sin TallerId a propósito: el taller sale siempre del token.
public record VehiculoEntrada(
    string? Patente, Guid TipoVehiculoId, string? Marca, string? Modelo, int? Anio,
    string? ContactoNombre, string? ContactoTelefono);

public record TipoVehiculoRespuesta(Guid Id, string Nombre);

public static class Patentes
{
    /// <summary>Mayúsculas y sin espacios ni guiones: "af 402-qp" → "AF402QP".</summary>
    public static string Normalizar(string? patente) =>
        new string((patente ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}

public static class VehiculoEndpoints
{
    public static void MapVehiculoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/vehiculos").RequireAuthorization();

        grupo.MapGet("/", async (string? patente, TallerDbContext db) =>
        {
            var consulta = db.Vehiculos.AsQueryable();
            if (!string.IsNullOrWhiteSpace(patente))
            {
                var p = Patentes.Normalizar(patente);
                consulta = consulta.Where(v => v.Patente == p);
            }
            return Results.Ok(await Proyectar(consulta.OrderByDescending(v => v.FechaAlta)).ToListAsync());
        });

        grupo.MapGet("/{id:guid}", async (Guid id, TallerDbContext db) =>
        {
            var vehiculo = await Proyectar(db.Vehiculos.Where(v => v.Id == id)).FirstOrDefaultAsync();
            return vehiculo is null ? Results.NotFound() : Results.Ok(vehiculo);
        });

        // RF-07: solo lectura. El frontend muestra el aviso y pide confirmación; nunca se autocompleta en silencio.
        grupo.MapGet("/buscar-por-patente/{patente}", async (string patente, TallerDbContext db) =>
        {
            var p = Patentes.Normalizar(patente);
            var vehiculo = await Proyectar(db.Vehiculos.Where(v => v.Patente == p).OrderByDescending(v => v.FechaAlta))
                .FirstOrDefaultAsync();
            return vehiculo is null ? Results.NotFound() : Results.Ok(vehiculo);
        });

        grupo.MapPost("/", async (VehiculoEntrada entrada, TallerDbContext db) =>
        {
            var errores = await Validar(entrada, db);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var vehiculo = new Vehiculo
            {
                Id = Guid.CreateVersion7(),
                FechaAlta = DateTime.UtcNow,
                // TallerId NO se setea acá: lo completa TallerDbContext.SaveChangesAsync.
            };
            Aplicar(vehiculo, entrada);
            db.Vehiculos.Add(vehiculo);
            await db.SaveChangesAsync();

            var respuesta = await Proyectar(db.Vehiculos.Where(v => v.Id == vehiculo.Id)).FirstAsync();
            return Results.Created($"/api/vehiculos/{vehiculo.Id}", respuesta);
        });

        grupo.MapPut("/{id:guid}", async (Guid id, VehiculoEntrada entrada, TallerDbContext db) =>
        {
            var vehiculo = await db.Vehiculos.FirstOrDefaultAsync(v => v.Id == id);
            if (vehiculo is null) return Results.NotFound();

            var errores = await Validar(entrada, db);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            Aplicar(vehiculo, entrada);
            await db.SaveChangesAsync();

            return Results.Ok(await Proyectar(db.Vehiculos.Where(v => v.Id == id)).FirstAsync());
        });

        app.MapGet("/api/tipos-vehiculo", async (TallerDbContext db) =>
            Results.Ok(await db.TiposVehiculo.Where(t => t.Activo).OrderBy(t => t.Nombre)
                .Select(t => new TipoVehiculoRespuesta(t.Id, t.Nombre)).ToListAsync()))
            .RequireAuthorization();
    }

    private static IQueryable<VehiculoRespuesta> Proyectar(IQueryable<Vehiculo> consulta) =>
        consulta.Select(v => new VehiculoRespuesta(
            v.Id, v.Patente, v.TipoVehiculoId, v.TipoVehiculo!.Nombre, v.Marca, v.Modelo,
            v.Anio, v.ContactoNombre, v.ContactoTelefono));

    private static void Aplicar(Vehiculo v, VehiculoEntrada e)
    {
        v.Patente = Patentes.Normalizar(e.Patente);
        v.TipoVehiculoId = e.TipoVehiculoId;
        v.Marca = Limpiar(e.Marca);
        v.Modelo = Limpiar(e.Modelo);
        v.Anio = e.Anio;
        v.ContactoNombre = Limpiar(e.ContactoNombre);
        v.ContactoTelefono = Limpiar(e.ContactoTelefono);
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static async Task<Dictionary<string, string[]>> Validar(VehiculoEntrada e, TallerDbContext db)
    {
        var errores = new Dictionary<string, string[]>();
        var patente = Patentes.Normalizar(e.Patente);
        if (patente.Length == 0) errores["patente"] = ["La patente es obligatoria."];
        else if (patente.Length > 10) errores["patente"] = ["La patente es demasiado larga."];

        if (!await db.TiposVehiculo.AnyAsync(t => t.Id == e.TipoVehiculoId && t.Activo))
            errores["tipoVehiculoId"] = ["El tipo de vehículo no es válido."];

        if (e.Anio is { } anio && (anio < 1900 || anio > DateTime.UtcNow.Year + 1))
            errores["anio"] = ["El año no es válido."];

        if (Limpiar(e.Marca)?.Length > 60) errores["marca"] = ["La marca es demasiado larga."];
        if (Limpiar(e.Modelo)?.Length > 60) errores["modelo"] = ["El modelo es demasiado largo."];
        if (Limpiar(e.ContactoNombre)?.Length > 120) errores["contactoNombre"] = ["El nombre es demasiado largo."];
        if (Limpiar(e.ContactoTelefono)?.Length > 40) errores["contactoTelefono"] = ["El teléfono es demasiado largo."];
        return errores;
    }
}
