using UnityEngine;

public class BotonComprar : MonoBehaviour
{
    public int precio;
    public int indiceSlot;
    public TiendaUI tiendaUI;

    public void Comprar()
    {
        tiendaUI.Comprar(precio, indiceSlot);
        tiendaUI.CerrarTienda();
    }
}