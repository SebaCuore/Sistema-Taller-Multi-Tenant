using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data.Entidades;

public class Vehiculo : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Patente { get; set; } = "";
    public Guid TipoVehiculoId { get; set; }
    public TipoVehiculo? TipoVehiculo { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int? Anio { get; set; }
    public string? ContactoNombre { get; set; }
    public string? ContactoTelefono { get; set; }
    public DateTime FechaAlta { get; set; }
}