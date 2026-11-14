using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.Stock;

public record ProductoRespuesta(Guid Id, string Nombre, decimal Precio, int StockActual);
// Sin TallerId: sale del token.
public record ProductoEntrada(string? Nombre, decimal? Precio, int? StockActual);

public static class ProductoEndpoints
{
    public static void MapProductoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/productos").RequireAuthorization();

        // RF-24 / RF-25. Solo los productos vigentes (los eliminados quedan inactivos para no romper las ventas viejas).
        grupo.MapGet("/", async (TallerDbContext db) =>
            Results.Ok(await db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre)
                .Select(p => new ProductoRespuesta(p.Id, p.Nombre, p.Precio, p.StockActual)).ToListAsync()));

        grupo.MapPost("/", async (ProductoEntrada entrada, TallerDbContext db) =>
        {
            var errores = Validar(entrada);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var producto = new Producto
            {
                Id = Guid.CreateVersion7(),
                Nombre = entrada.Nombre!.Trim(),
                Precio = entrada.Precio!.Value,
                StockActual = entrada.StockActual ?? 0,
                Activo = true,
            };
            db.Productos.Add(producto);
            await db.SaveChangesAsync();
            return Results.Created($"/api/productos/{producto.Id}", Proyectar(producto));
        });

        grupo.MapPut("/{id:guid}", async (Guid id, ProductoEntrada entrada, TallerDbContext db) =>
        {
            var producto = await db.Productos.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (producto is null) return Results.NotFound();
            var errores = Validar(entrada);
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            producto.Nombre = entrada.Nombre!.Trim();
            producto.Precio = entrada.Precio!.Value;
            if (entrada.StockActual is { } stock) producto.StockActual = stock;
            await db.SaveChangesAsync();
            return Results.Ok(Proyectar(producto));
        });

        grupo.MapDelete("/{id:guid}", async (Guid id, TallerDbContext db) =>
        {
            var producto = await db.Productos.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (producto is null) return Results.NotFound();
            producto.Activo = false; // baja lógica: el historial de ventas lo sigue mostrando
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static ProductoRespuesta Proyectar(Producto p) => new(p.Id, p.Nombre, p.Precio, p.StockActual);

    private static Dictionary<string, string[]> Validar(ProductoEntrada e)
    {
        var errores = new Dictionary<string, string[]>();
        var nombre = e.Nombre?.Trim() ?? "";
        if (nombre.Length == 0) errores["nombre"] = ["El nombre es obligatorio."];
        else if (nombre.Length > 150) errores["nombre"] = ["El nombre es demasiado largo."];
        if (e.Precio is not { } precio || precio < 0 || precio > 9_999_999_999m) errores["precio"] = ["El precio no es válido."];
        if (e.StockActual is < -1_000_000 or > 1_000_000) errores["stockActual"] = ["La cantidad no es válida."];
        return errores;
    }
}
