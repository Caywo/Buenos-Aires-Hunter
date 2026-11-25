
using UnityEngine;

public enum TipoArma
{
    Pistola,
    Escopeta,
    Rifle
}

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nombre;
    public GameObject prefabEnMano;
    public Sprite icono;

    [Header("Arma")]
    public TipoArma tipoArma;

    [Header("Consumible")]
    public bool esConsumible;
    public int cantidadInicial = 1;
    public bool arrancaDesbloqueado = false;

    public GameObject prefabUso;
    public bool esWachin = false;
}
