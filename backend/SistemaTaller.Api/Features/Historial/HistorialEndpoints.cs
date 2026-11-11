using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;
using SistemaTaller.Api.Features.OrdenesTrabajo;
using SistemaTaller.Api.Features.Vehiculos;

namespace SistemaTaller.Api.Features.Historial;

public record HistorialServicio(
    Guid OrdenId, Guid VehiculoId, string Patente, string Tipo, string? Marca, string? Modelo,
    DateTime FechaIngreso, DateTime FechaEntrega, decimal Total, List<ItemRespuesta> Servicios);

public static class HistorialEndpoints
{
    private const int Maximo = 200;

    public static void MapHistorialEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/historial").RequireAuthorization();

        // RF-22 y RF-23: órdenes entregadas, la más reciente primero; ?patente= filtra (también por parte de la patente).
        grupo.MapGet("/servicios", async (string? patente, TallerDbContext db) =>
        {
            var ordenes = db.OrdenesTrabajo.Where(o => o.Estado == EstadoOrden.Entregado);
            var consulta =
                from o in ordenes
                join v in db.Vehiculos on o.VehiculoId equals v.Id
                select new { o, v };

            var p = Patentes.Normalizar(patente);
            if (p.Length > 0) consulta = consulta.Where(x => x.v.Patente.Contains(p));

            var cabeceras = await consulta
                .OrderByDescending(x => x.o.FechaEntrega)
                .Take(Maximo)
                .Select(x => new
                {
                    x.o.Id, VehiculoId = x.v.Id, x.v.Patente, Tipo = x.v.TipoVehiculo!.Nombre, x.v.Marca, x.v.Modelo,
                    x.o.FechaIngreso, FechaEntrega = x.o.FechaEntrega!.Value,
                })
                .ToListAsync();

            var ids = cabeceras.Select(c => c.Id).ToList();
            var items = await (
                from s in db.OrdenesTrabajoServicio.Where(s => ids.Contains(s.OrdenTrabajoId))
                join sv in db.Servicios on s.ServicioId equals sv.Id // un servicio desactivado sigue visible acá
                orderby s.Id
                select new { s.OrdenTrabajoId, Item = new ItemRespuesta(s.Id, sv.Id, sv.Nombre, s.Posicion, s.Lado, s.PrecioFinal, s.Notas) })
                .ToListAsync();
            var porOrden = items.ToLookup(i => i.OrdenTrabajoId, i => i.Item);

            var respuesta = cabeceras.Select(c =>
            {
                var servicios = porOrden[c.Id].ToList();
                return new HistorialServicio(
                    c.Id, c.VehiculoId, c.Patente, c.Tipo, c.Marca, c.Modelo, c.FechaIngreso, c.FechaEntrega,
                    servicios.Sum(i => i.PrecioFinal), servicios);
            }).ToList();

            return Results.Ok(respuesta);
        });
    }
}
