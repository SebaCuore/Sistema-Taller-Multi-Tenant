namespace SistemaTaller.Api.Data.Entidades;

public class TipoVehiculo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
}