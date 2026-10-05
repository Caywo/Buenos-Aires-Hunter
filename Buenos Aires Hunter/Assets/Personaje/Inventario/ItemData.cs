using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nombre;
    public GameObject prefabEnMano;
    public Sprite icono;

    public bool esConsumible;
    public int cantidadInicial = 1;
    public bool arrancaDesbloqueado = false;

    public GameObject prefabUso;
    public bool esWachin = false;
}