using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.OrdenesTrabajo;

public record ItemEntrada(Guid ServicioId, Posicion? Posicion, Lado? Lado, decimal? PrecioFinal, string? Notas);
// Sin TallerId ni UsuarioId: salen del token.
public record OrdenAlta(Guid VehiculoId, string? Notas, List<ItemEntrada>? Servicios);
public record EstadoCambio(EstadoOrden Estado);

public record OrdenResumen(
    Guid Id, Guid VehiculoId, string Patente, string Tipo, string? Marca, string? Modelo,
    EstadoOrden Estado, DateTime FechaIngreso, int CantidadServicios, decimal Total);

public record ItemRespuesta(
    Guid Id, Guid ServicioId, string Servicio, Posicion? Posicion, Lado? Lado, decimal PrecioFinal, string? Notas);

public record OrdenDetalle(
    Guid Id, Guid VehiculoId, string Patente, string Tipo, string? Marca, string? Modelo,
    string? ContactoNombre, string? ContactoTelefono, EstadoOrden Estado,
    DateTime FechaIngreso, DateTime? FechaEntrega, string? Notas, decimal Total, List<ItemRespuesta> Servicios);

public static class OrdenEndpoints
{
    public static void MapOrdenEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/ordenes").RequireAuthorization();

        // RF-14. Sin ?estado devuelve las órdenes en curso (todas menos Entregado).
        grupo.MapGet("/", async (EstadoOrden? estado, TallerDbContext db) =>
        {
            var ordenes = db.OrdenesTrabajo.AsQueryable();
            ordenes = estado is { } e ? ordenes.Where(o => o.Estado == e) : ordenes.Where(o => o.Estado != EstadoOrden.Entregado);

            var lista = await (
                from o in ordenes
                join v in db.Vehiculos on o.VehiculoId equals v.Id
                orderby o.FechaIngreso
                select new OrdenResumen(
                    o.Id, v.Id, v.Patente, v.TipoVehiculo!.Nombre, v.Marca, v.Modelo, o.Estado, o.FechaIngreso,
                    o.Servicios.Count, o.Servicios.Sum(s => (decimal?)s.PrecioFinal) ?? 0m))
                .ToListAsync();
            return Results.Ok(lista);
        });

        grupo.MapGet("/{id:guid}", async (Guid id, TallerDbContext db) =>
        {
            var detalle = await Detalle(db, id);
            return detalle is null ? Results.NotFound() : Results.Ok(detalle);
        });

        // RF-15 a RF-18
        grupo.MapPost("/", async (OrdenAlta alta, ClaimsPrincipal user, TallerDbContext db) =>
        {
            var errores = new Dictionary<string, string[]>();
            if (!await db.Vehiculos.AnyAsync(v => v.Id == alta.VehiculoId))
                errores["vehiculoId"] = ["El vehículo no existe."];
            if (alta.Servicios is null or { Count: 0 })
                errores["servicios"] = ["La orden necesita al menos un servicio."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var orden = new OrdenTrabajo
            {
                Id = Guid.CreateVersion7(),
                VehiculoId = alta.VehiculoId,
                UsuarioId = Guid.Parse(user.FindFirst("sub")!.Value),
                Estado = EstadoOrden.Ingresado,
                FechaIngreso = DateTime.UtcNow,
                Notas = Limpiar(alta.Notas),
            };

            foreach (var item in alta.Servicios!)
            {
                var (nuevo, error) = await ArmarItem(db, item);
                if (error is not null) return Results.ValidationProblem(error);
                nuevo!.OrdenTrabajoId = orden.Id;
                orden.Servicios.Add(nuevo);
            }

            db.OrdenesTrabajo.Add(orden);
            await db.SaveChangesAsync();
            return Results.Created($"/api/ordenes/{orden.Id}", await Detalle(db, orden.Id));
        });

        // RF-19
        grupo.MapPost("/{id:guid}/servicios", async (Guid id, ItemEntrada item, TallerDbContext db) =>
        {
            var orden = await db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id);
            if (orden is null) return Results.NotFound();
            if (orden.Estado == EstadoOrden.Entregado) return OrdenCerrada();

            var (nuevo, error) = await ArmarItem(db, item);
            if (error is not null) return Results.ValidationProblem(error);
            nuevo!.OrdenTrabajoId = id;
            db.OrdenesTrabajoServicio.Add(nuevo);
            await db.SaveChangesAsync();
            return Results.Ok(await Detalle(db, id));
        });

        grupo.MapDelete("/{id:guid}/servicios/{itemId:guid}", async (Guid id, Guid itemId, TallerDbContext db) =>
        {
            var orden = await db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id);
            if (orden is null) return Results.NotFound();
            if (orden.Estado == EstadoOrden.Entregado) return OrdenCerrada();

            var item = await db.OrdenesTrabajoServicio.FirstOrDefaultAsync(s => s.Id == itemId && s.OrdenTrabajoId == id);
            if (item is null) return Results.NotFound();
            db.OrdenesTrabajoServicio.Remove(item);
            await db.SaveChangesAsync();
            return Results.Ok(await Detalle(db, id));
        });

        // RF-20: se puede pasar de Ingresado a cualquier estado en curso; los intermedios son opcionales.
        grupo.MapPatch("/{id:guid}/estado", async (Guid id, EstadoCambio cambio, TallerDbContext db) =>
        {
            var orden = await db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id);
            if (orden is null) return Results.NotFound();
            if (orden.Estado == EstadoOrden.Entregado) return OrdenCerrada();
            if (!Enum.IsDefined(cambio.Estado))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["estado"] = ["Estado inválido."] });
            if (cambio.Estado == EstadoOrden.Entregado)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["estado"] = ["Para entregar usá /finalizar."] });

            orden.Estado = cambio.Estado;
            await db.SaveChangesAsync();
            return Results.Ok(await Detalle(db, id));
        });

        // RF-21: pasa a Entregado desde cualquier estado en curso, sin exigir pasos intermedios.
        grupo.MapPost("/{id:guid}/finalizar", async (Guid id, TallerDbContext db) =>
        {
            var orden = await db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id);
            if (orden is null) return Results.NotFound();
            if (orden.Estado == EstadoOrden.Entregado) return OrdenCerrada();

            orden.Estado = EstadoOrden.Entregado;
            orden.FechaEntrega = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(await Detalle(db, id));
        });
    }

    private static IResult OrdenCerrada() =>
        Results.Problem(statusCode: 409, title: "La orden ya fue entregada y no se puede modificar.");

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static async Task<(OrdenTrabajoServicio? Item, Dictionary<string, string[]>? Error)> ArmarItem(
        TallerDbContext db, ItemEntrada e)
    {
        var servicio = await db.Servicios.FirstOrDefaultAsync(s => s.Id == e.ServicioId);
        if (servicio is null || !servicio.Activo)
            return (null, new() { ["servicioId"] = ["El servicio no existe o está desactivado."] });
        // Posición y lado solo se aceptan si el servicio los declara.
        if (e.Posicion is not null && !servicio.AplicaPosicion)
            return (null, new() { ["posicion"] = [$"{servicio.Nombre} no usa posición."] });
        if (e.Lado is not null && !servicio.AplicaLado)
            return (null, new() { ["lado"] = [$"{servicio.Nombre} no usa lado."] });
        if (e.Posicion is { } p && !Enum.IsDefined(p) || e.Lado is { } l && !Enum.IsDefined(l))
            return (null, new() { ["posicion"] = ["Valor inválido."] });
        if (e.PrecioFinal is < 0)
            return (null, new() { ["precioFinal"] = ["El precio no puede ser negativo."] });

        return (new OrdenTrabajoServicio
        {
            Id = Guid.CreateVersion7(),
            ServicioId = servicio.Id,
            Posicion = e.Posicion,
            Lado = e.Lado,
            // El precio de referencia solo precarga; lo que manda es lo que edita el empleado.
            PrecioFinal = e.PrecioFinal ?? servicio.PrecioReferencia ?? 0m,
            Notas = Limpiar(e.Notas),
        }, null);
    }

    private static async Task<OrdenDetalle?> Detalle(TallerDbContext db, Guid id)
    {
        var cabecera = await (
            from o in db.OrdenesTrabajo.Where(o => o.Id == id)
            join v in db.Vehiculos on o.VehiculoId equals v.Id
            select new
            {
                o.Id, VehiculoId = v.Id, v.Patente, Tipo = v.TipoVehiculo!.Nombre, v.Marca, v.Modelo,
                v.ContactoNombre, v.ContactoTelefono, o.Estado, o.FechaIngreso, o.FechaEntrega, o.Notas,
            }).FirstOrDefaultAsync();
        if (cabecera is null) return null;

        var items = await (
            from s in db.OrdenesTrabajoServicio.Where(s => s.OrdenTrabajoId == id)
            join sv in db.Servicios on s.ServicioId equals sv.Id
            orderby s.Id // Guid v7: orden de carga
            select new ItemRespuesta(s.Id, sv.Id, sv.Nombre, s.Posicion, s.Lado, s.PrecioFinal, s.Notas))
            .ToListAsync();

        return new OrdenDetalle(
            cabecera.Id, cabecera.VehiculoId, cabecera.Patente, cabecera.Tipo, cabecera.Marca, cabecera.Modelo,
            cabecera.ContactoNombre, cabecera.ContactoTelefono, cabecera.Estado, cabecera.FechaIngreso,
            cabecera.FechaEntrega, cabecera.Notas, items.Sum(i => i.PrecioFinal), items);
    }
}
