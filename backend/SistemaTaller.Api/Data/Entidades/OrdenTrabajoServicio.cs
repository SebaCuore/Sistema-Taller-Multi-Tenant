using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data.Entidades;

public class OrdenTrabajoServicio : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid OrdenTrabajoId { get; set; }
    public Guid ServicioId { get; set; }
    public Posicion? Posicion { get; set; }
    public Lado? Lado { get; set; }
    public decimal PrecioFinal { get; set; }
    public string? Notas { get; set; }
}
