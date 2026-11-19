using System;
using UnityEngine;
using UnityEngine.AI;

public enum NivelDificultad
{
    Normal = 0,
    Dificil = 1
}

/// <summary>Multiplicadores de una dificultad. 1 = sin cambios.</summary>
[Serializable]
public class PerfilDificultad
{
    [Header("Enemigos")]
    [Tooltip("Multiplica la vida de los enemigos y del jefe.")]
    public float vidaEnemigos = 1f;
    [Tooltip("Multiplica el daño que los enemigos hacen a los jugadores.")]
    public float danoEnemigos = 1f;
    [Tooltip("Multiplica la velocidad de movimiento de los enemigos.")]
    public float velocidadEnemigos = 1f;

    [Header("Oleadas")]
    [Tooltip("Multiplica la cantidad de enemigos de cada oleada.")]
    public float cantidadEnemigos = 1f;
    [Tooltip("Multiplica el máximo de enemigos vivos a la vez.")]
    public float maxEnemigosVivos = 1f;
    [Tooltip("Multiplica el dinero que da cada baja.")]
    public float recompensaPorBaja = 1f;

    [Header("Jugador")]
    [Tooltip("Vida que recupera el jugador al eliminar un enemigo (0 = nada).")]
    public float curacionPorBaja = 5f;
}

/// <summary>Dificultad elegida y su perfil. Se guarda en PlayerPrefs (por defecto, Normal).</summary>
public static class Dificultad
{
    private const string ClavePrefs = "dificultad_nivel";

    private static ConfigDificultad config;
    private static NivelDificultad nivel;
    private static bool nivelCargado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        config = null;
        nivelCargado = false;
    }

    public static NivelDificultad Actual
    {
        get
        {
            if (!nivelCargado)
            {
                int guardado = Mathf.Clamp(PlayerPrefs.GetInt(ClavePrefs, 0), 0, 1);
                nivel = (NivelDificultad)guardado;
                nivelCargado = true;
            }

            return nivel;
        }
        set
        {
            nivel = value;
            nivelCargado = true;
            PlayerPrefs.SetInt(ClavePrefs, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static bool EsDificil => Actual == NivelDificultad.Dificil;

    /// <summary>Perfil de la dificultad actual. Lee el asset "ConfigDificultad" de Resources (o usa valores por defecto).</summary>
    public static PerfilDificultad Perfil
    {
        get
        {
            if (config == null)
            {
                config = Resources.Load<ConfigDificultad>("ConfigDificultad");

                if (config == null)
                    config = ScriptableObject.CreateInstance<ConfigDificultad>();
            }

            return EsDificil ? config.dificil : config.normal;
        }
    }

    /// <summary>SOLO SERVIDOR. Ajusta la velocidad de un enemigo que acaba de aparecer.</summary>
    public static void AplicarAEnemigo(GameObject enemigo)
    {
        // El jefe maneja sus velocidades en BossAI.
        if (enemigo.GetComponent<BossAI>() != null)
            return;

        float velocidad = Perfil.velocidadEnemigos;

        if (Mathf.Approximately(velocidad, 1f))
            return;

        NavMeshAgent agente = enemigo.GetComponent<NavMeshAgent>();

        if (agente != null)
        {
            agente.speed *= velocidad;
            agente.acceleration *= velocidad;
        }
    }
}