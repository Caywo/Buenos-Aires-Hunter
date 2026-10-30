using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SOLO SERVIDOR (lo usan los enemigos). Lista de jugadores vivos con caché compartida,
/// para que muchos enemigos no hagan una búsqueda cada uno.
/// Identifica jugadores por PlayerCombat
public static class ObjetivosJugadores
{
    private const float IntervaloRefresco = 0.5f;

    private static readonly List<Health> jugadores = new List<Health>();
    private static float proximoRefresco;

    private static void Refrescar()
    {
        // La segunda condición cubre reinicios de Time.time (Play Mode sin recargar dominio).
        if (Time.time < proximoRefresco && Time.time >= proximoRefresco - IntervaloRefresco)
            return;

        proximoRefresco = Time.time + IntervaloRefresco;
        jugadores.Clear();

        PlayerCombat[] encontrados = Object.FindObjectsByType<PlayerCombat>(FindObjectsSortMode.None);

        foreach (PlayerCombat pc in encontrados)
        {
            Health h = pc.GetComponent<Health>();
            if (h != null)
                jugadores.Add(h);
        }
    }

    /// Jugador vivo más cercano en el plano horizontal, o null si no hay.
    public static Health MasCercano(Vector3 desde)
    {
        Refrescar();

        Health mejor = null;
        float mejorSq = float.MaxValue;

        foreach (Health h in jugadores)
        {
            if (h == null || h.EstaMuerto)
                continue;

            Vector3 delta = h.transform.position - desde;
            delta.y = 0f;

            float sq = delta.sqrMagnitude;
            if (sq < mejorSq)
            {
                mejorSq = sq;
                mejor = h;
            }
        }

        return mejor;
    }
}