using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Bondi : Interactuable
{
    public int precioPasaje;
    public string siguienteMapa;
    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer) return;

        if (jugador.PagarConSube(precioPasaje))
        {
            Debug.Log("Jugador pagó");
            SaveManager.Instance.GuardarPartida(siguienteMapa);
            NetworkManager.Singleton.SceneManager.LoadScene(siguienteMapa, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("Falta saldo o SUBE en mano");
        }
    }
}
