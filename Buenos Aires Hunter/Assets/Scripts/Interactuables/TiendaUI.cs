using UnityEngine;

public class TiendaUI : MonoBehaviour
{
    public GameObject panelTienda;
    private PlayerInventory jugadorActual;

    public void AbrirTienda(PlayerInventory jugador)
    {
        jugadorActual = jugador;
        panelTienda.SetActive(true);

        jugador.GetComponent<PlayerLook>().inputBloqueado = true;
        jugador.inputBloqueado = true;
        jugador.GetComponent<PlayerMotor>().inputBloqueado = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        panelTienda.SetActive(false);

        if (jugadorActual != null)
        {
            jugadorActual.GetComponent<PlayerLook>().inputBloqueado = false;
            jugadorActual.inputBloqueado = false;
            jugadorActual.GetComponent<PlayerMotor>().inputBloqueado = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Comprar(int precio, int indice)
    {
        if (jugadorActual == null) return;
        jugadorActual.IntentarComprarRpc(precio, indice);
    }
}