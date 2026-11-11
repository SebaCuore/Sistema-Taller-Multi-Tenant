namespace SistemaTaller.Api.Common;

/// <summary>Taller del usuario autenticado, leído del claim <c>taller_id</c> del JWT.</summary>
public interface ITallerActual
{
    /// <summary>Lanza <see cref="InvalidOperationException"/> si no hay un taller autenticado.</summary>
    Guid TallerId { get; }
}
