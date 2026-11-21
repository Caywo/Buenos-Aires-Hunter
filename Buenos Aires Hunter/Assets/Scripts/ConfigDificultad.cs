using UnityEngine;

/// <summary>Números de cada dificultad. Se edita desde el Inspector, sin tocar código.</summary>
[CreateAssetMenu(fileName = "ConfigDificultad", menuName = "Balance/Config de dificultad")]
public class ConfigDificultad : ScriptableObject
{
    public PerfilDificultad normal = new PerfilDificultad();

    public PerfilDificultad dificil = new PerfilDificultad
    {
        vidaEnemigos = 1.3f,
        danoEnemigos = 1.4f,
        velocidadEnemigos = 1.1f,
        cantidadEnemigos = 1.3f,
        maxEnemigosVivos = 1.3f,
        recompensaPorBaja = 0.85f,
        curacionPorBaja = 2f
    };
}
