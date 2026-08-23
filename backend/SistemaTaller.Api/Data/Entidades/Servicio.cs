namespace SistemaTaller.Api.Data.Entidades;

public class Servicio : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Nombre { get; set; } = "";
    public CategoriaServicio CategoriaVehiculo { get; set; }
    public bool AplicaPosicion { get; set; }
    public bool AplicaLado { get; set; }
    public decimal? PrecioReferencia { get; set; }
    public bool Activo { get; set; } = true;
}