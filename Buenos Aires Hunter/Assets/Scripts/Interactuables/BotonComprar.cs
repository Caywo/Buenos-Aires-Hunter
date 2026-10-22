using UnityEngine;

public class BotonComprar : MonoBehaviour
{
    public int precio;
    public int indiceSlot;
    public bool esMejoraVelocidad; // nuevo
    public TiendaUI tiendaUI;

    public void Comprar()
    {
        if (esMejoraVelocidad)
        {
            tiendaUI.ComprarMejoraVelocidad(precio);
        }
        else
        {
            tiendaUI.Comprar(precio, indiceSlot);
        }
        tiendaUI.CerrarTienda();
    }
}