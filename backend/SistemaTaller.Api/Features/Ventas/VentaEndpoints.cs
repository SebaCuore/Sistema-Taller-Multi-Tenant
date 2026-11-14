using Microsoft.EntityFrameworkCore;
using SistemaTaller.Api.Data;
using SistemaTaller.Api.Data.Entidades;

namespace SistemaTaller.Api.Features.Ventas;

public record VentaAlta(Guid ProductoId, int? Cantidad, decimal? PrecioUnitario);

public record VentaRespuesta(
    Guid Id, Guid ProductoId, string Producto, int Cantidad, decimal PrecioUnitario, decimal Total,
    DateTime Fecha, int StockRestante, bool SinStockSuficiente);

public record VentaHistorial(
    Guid Id, Guid ProductoId, string Producto, int Cantidad, decimal PrecioUnitario, decimal Total, DateTime Fecha);

public static class ReglasDeStock
{
    /// <summary>RF-28: la venta nunca se bloquea; esto solo decide si se muestra la advertencia suave.</summary>
    public static bool SinStockSuficiente(int stockAntesDeVender, int cantidad) => stockAntesDeVender < cantidad;
}

public static class VentaEndpoints
{
    private const int Maximo = 200;

    public static void MapVentaEndpoints(this WebApplication app)
    {
        // RF-26 a RF-29
        app.MapPost("/api/ventas", async (VentaAlta alta, TallerDbContext db) =>
        {
            var errores = new Dictionary<string, string[]>();
            var producto = await db.Productos.FirstOrDefaultAsync(p => p.Id == alta.ProductoId && p.Activo);
            if (producto is null) errores["productoId"] = ["El producto no existe."];
            var cantidad = alta.Cantidad ?? 1;
            if (cantidad is < 1 or > 100_000) errores["cantidad"] = ["La cantidad tiene que ser 1 o más."];
            if (alta.PrecioUnitario is < 0 or > 9_999_999_999m) errores["precioUnitario"] = ["El precio no es válido."];
            if (errores.Count > 0) return Results.ValidationProblem(errores);

            var venta = new Venta
            {
                Id = Guid.CreateVersion7(),
                ProductoId = producto!.Id,
                Cantidad = cantidad,
                // Se congela el precio del momento: cambios posteriores en Producto.Precio no lo tocan.
                PrecioUnitario = alta.PrecioUnitario ?? producto.Precio,
                Fecha = DateTime.UtcNow,
            };

            await using var transaccion = await db.Database.BeginTransactionAsync();
            db.Ventas.Add(venta);
            await db.SaveChangesAsync();
            // Descuento atómico en la base (RF-27): dos ventas simultáneas no se pisan. Puede quedar negativo (RF-28).
            await db.Productos.Where(p => p.Id == producto.Id)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.StockActual, x => x.StockActual - cantidad));
            var stockRestante = await db.Productos.Where(p => p.Id == producto.Id).Select(p => p.StockActual).SingleAsync();
            await transaccion.CommitAsync();

            return Results.Created($"/api/ventas/{venta.Id}", new VentaRespuesta(
                venta.Id, producto.Id, producto.Nombre, venta.Cantidad, venta.PrecioUnitario,
                venta.Cantidad * venta.PrecioUnitario, venta.Fecha, stockRestante,
                ReglasDeStock.SinStockSuficiente(stockRestante + cantidad, cantidad)));
        }).RequireAuthorization();

        app.MapGet("/api/historial/ventas", async (TallerDbContext db) =>
        {
            var lista = await (
                from v in db.Ventas
                join p in db.Productos on v.ProductoId equals p.Id // incluye productos dados de baja
                orderby v.Fecha descending
                select new VentaHistorial(v.Id, p.Id, p.Nombre, v.Cantidad, v.PrecioUnitario, v.Cantidad * v.PrecioUnitario, v.Fecha))
                .Take(Maximo).ToListAsync();
            return Results.Ok(lista);
        }).RequireAuthorization();
    }
}
