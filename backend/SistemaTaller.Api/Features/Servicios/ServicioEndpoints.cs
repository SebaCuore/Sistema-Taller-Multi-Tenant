using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;

namespace SistemaTaller.Api.Features.Servicios;

public record ServicioRespuesta(
    Guid Id, string Nombre, string CategoriaVehiculo, bool AplicaPosicion, bool AplicaLado,
    decimal? PrecioReferencia, bool Activo);

public static class ServicioEndpoints
{
    // Solo lectura por ahora (la Fase 3 necesita el selector). Alta y activar/desactivar llegan en la Fase 5.
    public static void MapServicioEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/servicios").RequireAuthorization();

        grupo.MapGet("/", async (bool? soloActivos, TallerDbContext db) =>
        {
            var consulta = db.Servicios.AsQueryable();
            if (soloActivos == true) consulta = consulta.Where(s => s.Activo);
            var lista = await consulta.OrderBy(s => s.Nombre)
                .Select(s => new ServicioRespuesta(
                    s.Id, s.Nombre, s.CategoriaVehiculo.ToString(), s.AplicaPosicion, s.AplicaLado,
                    s.PrecioReferencia, s.Activo))
                .ToListAsync();
            return Results.Ok(lista);
        });
    }
}
