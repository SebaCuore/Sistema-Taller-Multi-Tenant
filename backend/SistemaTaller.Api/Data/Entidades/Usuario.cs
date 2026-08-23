using SistemaTaller.Api.Common;

namespace SistemaTaller.Api.Data.Entidades;

public class Usuario : IPerteneceATaller
{
    public Guid Id { get; set; }
    public Guid TallerId { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public RolUsuario Rol { get; set; } = RolUsuario.Admin;
    public bool Activo { get; set; } = true;
    public DateTime FechaAlta { get; set; }
}