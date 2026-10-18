using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bondi : Interactuable
{
    [SerializeField] private int precioPasaje = 0;
    [SerializeField] private string siguienteMapa = "MapaExtraccion2";

    private bool extraccionIniciada;

    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer || extraccionIniciada || jugador == null)
            return;

        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.SceneManager == null)
            return;

        if (!jugador.PagarConSube(precioPasaje))
            return;

        extraccionIniciada = true;

        if (SaveManager.Instance != null)
            SaveManager.Instance.GuardarPartida(siguienteMapa);

        NetworkManager.Singleton.SceneManager.LoadScene(
            siguienteMapa,
            LoadSceneMode.Single
        );
    }
}