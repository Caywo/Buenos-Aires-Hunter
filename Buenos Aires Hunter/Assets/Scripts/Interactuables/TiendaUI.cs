using System.Security.Cryptography;
using UnityEngine;

public class TiendaUI : MonoBehaviour
{
    public GameObject panelTienda;
    private PlayerInventory jugadorActual;
    // Update is called once per frame
    public void AbrirTienda(PlayerInventory jugador)
    {
        jugadorActual = jugador;
        panelTienda.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        panelTienda.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Comprar(int precio)
    {
        if (jugadorActual == null) return;
        jugadorActual.IntentarPagarServerRpc(precio);
    }
}
