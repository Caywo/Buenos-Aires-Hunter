using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Bondi : Interactuable
{
    public int precioPasaje;
    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer) return;

        if (jugador.PagarConSube(precioPasaje))
        {
            Debug.Log("Jugador pagó");
            NetworkManager.Singleton.SceneManager.LoadScene("MenuPrincipal", LoadSceneMode.Single);
            NetworkManager.Singleton.Shutdown();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.Log("Falta saldo o SUBE en mano");
        }
    }
}
