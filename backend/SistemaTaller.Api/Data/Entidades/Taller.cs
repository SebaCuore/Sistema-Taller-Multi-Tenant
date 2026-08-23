namespace SistemaTaller.Api.Data.Entidades;

public class Taller
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}