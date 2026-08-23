using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data.Entidades;

public class OrdenTrabajo : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public Guid VehiculoId { get; set; }
    public Guid UsuarioId { get; set; }
    public EstadoOrden Estado { get; set; } = EstadoOrden.Ingresado;
    public DateTime FechaIngreso { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? Notas { get; set; }

    public List<OrdenTrabajoServicio> Servicios { get; set; } = [];
}