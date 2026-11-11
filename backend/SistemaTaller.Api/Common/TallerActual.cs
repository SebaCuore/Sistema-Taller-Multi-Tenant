namespace SistemaTaller.Api.Common;

public sealed class TallerActual(IHttpContextAccessor accessor) : ITallerActual
{
    // Se lee de forma perezosa: el DbContext se construye también en pedidos sin token
    // (POST /auth/login) y ahí no debe fallar; solo falla si alguien necesita el taller.
    public Guid TallerId
    {
        get
        {
            var claim = accessor.HttpContext?.User.FindFirst("taller_id")
                ?? throw new InvalidOperationException("El usuario autenticado no tiene taller_id en el token.");
            return Guid.Parse(claim.Value);
        }
    }
}

/// <summary>Taller fijo, para procesos sin usuario autenticado (comando crear-admin, migraciones, tests).</summary>
public sealed class TallerFijo(Guid tallerId) : ITallerActual
{
    public Guid TallerId { get; } = tallerId;
}
