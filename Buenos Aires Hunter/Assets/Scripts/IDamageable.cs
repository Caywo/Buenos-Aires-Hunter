/// <summary>
/// Todo lo que pueda recibir daño (jugadores, enemigos, barriles, torretas...) implementa esto.
/// </summary>
public interface IDamageable
{
    /// <param name="danio">Cantidad de daño.</param>
    /// <param name="atacanteId">ClientId del jugador atacante, o ReglasCombate.AtacanteNeutral (enemigos, entorno).</param>
    void TakeDamage(float danio, ulong atacanteId);
}

/// <summary>
/// Reglas globales de combate. El futuro modo duelo solo tiene que poner PvPActivo = true (en el servidor).
/// </summary>
public static class ReglasCombate
{
    public const ulong AtacanteNeutral = ulong.MaxValue;

    /// <summary>Si es false, los jugadores no se hacen daño entre sí (modo oleadas cooperativo).</summary>
    public static bool PvPActivo = false;
}
