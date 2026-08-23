using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data.Entidades;

public class Venta : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public DateTime Fecha { get; set; }
}
