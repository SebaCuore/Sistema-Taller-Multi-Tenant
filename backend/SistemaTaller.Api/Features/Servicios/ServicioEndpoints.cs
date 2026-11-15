using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.Servicios;

public record ServicioRespuesta(
    Guid Id, string Nombre, string CategoriaVehiculo, bool AplicaPosicion, bool AplicaLado,
    decimal? PrecioReferencia, bool Activo);

public record ServicioAlta(
    string? Nombre, CategoriaServicio? CategoriaVehiculo, bool AplicaPosicion, bool AplicaLado, decimal? PrecioReferencia);

/// <summary>Cambios parciales: solo se aplica lo que viene. <c>SinPrecio = true</c> borra el precio de referencia.</summary>
public record ServicioCambio(
    bool? Activo, decimal? PrecioReferencia, bool? SinPrecio,
    string? Nombre = null, CategoriaServicio? CategoriaVehiculo = null, bool? AplicaPosicion = null, bool? AplicaLado = null);

public static class ServicioEndpoints
{
    public static void MapServicioEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/servicios").RequireAuthorization();

        grupo.MapGet("/", async (bool? soloActivos, TallerDbContext db) =>
        {
            var consulta = db.Servicios.AsQueryable();
            if (soloActivos == true) consulta = consulta.Where(s => s.Activo);
            return Results.Ok(await consulta.OrderBy(s => s.Nombre).Select(s => Proyectar(s)).ToListAsync());
        });

        // RF-11 a RF-13: servicio propio del taller.
        grupo.MapPost("/", async (ServicioAlta alta, TallerDbContext db) =>
        {
            var errores = new Dictionary<string, string[]>();
            var nombre = alta.Nombre?.Trim() ?? "";
            if (nombre.Length == 0) errores["nombre"] = ["El nombre es obligatorio."];
            else if (nombre.Length > 120) errores["nombre"] = ["El nombre es demasiado largo."];
            else if (await db.Servicios.AnyAsync(s => s.Nombre.ToLower() == nombre.ToLower()))
                errores["nombre"] = ["Ya existe un servicio con ese nombre."];
            if (alta.CategoriaVehiculo is not { } categoria || !Enum.IsDefined(categoria))
                errores["categoriaVehiculo"] = ["Elegí Auto, Moto o Ambos."];
            if (!PrecioValido(alta.PrecioReferencia)) errores["precioReferencia"] = ["El precio no es válido."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var servicio = new Servicio
            {
                Id = Guid.CreateVersion7(),
                Nombre = nombre,
                CategoriaVehiculo = alta.CategoriaVehiculo!.Value,
                AplicaPosicion = alta.AplicaPosicion,
                AplicaLado = alta.AplicaLado,
                PrecioReferencia = alta.PrecioReferencia,
                Activo = true,
            };
            db.Servicios.Add(servicio);
            await db.SaveChangesAsync();
            return Results.Created($"/api/servicios/{servicio.Id}", Proyectar(servicio));
        });

        // RF-10: se activa/desactiva, nunca se borra (no hay DELETE). También permite corregir precio, nombre, tipo y posición/lado.
        grupo.MapPatch("/{id:guid}", async (Guid id, ServicioCambio cambio, TallerDbContext db) =>
        {
            var servicio = await db.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio is null) return Results.NotFound();
            var errores = new Dictionary<string, string[]>();
            if (!PrecioValido(cambio.PrecioReferencia)) errores["precioReferencia"] = ["El precio no es válido."];
            if (cambio.CategoriaVehiculo is { } cat && !Enum.IsDefined(cat))
                errores["categoriaVehiculo"] = ["Elegí Auto, Moto o Ambos."];
            var nombre = cambio.Nombre?.Trim();
            if (cambio.Nombre is not null)
            {
                if (nombre!.Length == 0) errores["nombre"] = ["El nombre es obligatorio."];
                else if (nombre.Length > 120) errores["nombre"] = ["El nombre es demasiado largo."];
                else if (await db.Servicios.AnyAsync(s => s.Id != id && s.Nombre.ToLower() == nombre.ToLower()))
                    errores["nombre"] = ["Ya existe un servicio con ese nombre."];
            }
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            // Corregir nombre, tipo de vehículo o posición/lado cambia el servicio también en las órdenes viejas (decisión del dueño).
            if (nombre is not null) servicio.Nombre = nombre;
            if (cambio.CategoriaVehiculo is { } categoria) servicio.CategoriaVehiculo = categoria;
            if (cambio.AplicaPosicion is { } aplicaPosicion) servicio.AplicaPosicion = aplicaPosicion;
            if (cambio.AplicaLado is { } aplicaLado) servicio.AplicaLado = aplicaLado;
            if (cambio.Activo is { } activo) servicio.Activo = activo;
            if (cambio.SinPrecio == true) servicio.PrecioReferencia = null;
            else if (cambio.PrecioReferencia is { } precio) servicio.PrecioReferencia = precio;
            await db.SaveChangesAsync();
            return Results.Ok(Proyectar(servicio));
        });
    }

    private static bool PrecioValido(decimal? precio) => precio is null or (>= 0 and <= 9_999_999_999m);

    private static ServicioRespuesta Proyectar(Servicio s) => new(
        s.Id, s.Nombre, s.CategoriaVehiculo.ToString(), s.AplicaPosicion, s.AplicaLado, s.PrecioReferencia, s.Activo);
}
