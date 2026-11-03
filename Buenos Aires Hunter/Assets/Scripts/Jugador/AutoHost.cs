using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class AutoHostEditor : MonoBehaviour
{
#if UNITY_EDITOR
    private void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("AutoHostEditor: no hay NetworkManager en la escena.");
            return;
        }

        if (nm.IsListening) return; // ya hay una partida en curso

        if (NetworkManagerPersistente.Instance != null)
            NetworkManagerPersistente.Instance.ConfigurarSpawnDeJugadores(false);

        nm.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        nm.StartHost();

        // El HUD arranca oculto y normalmente lo activa OnLoadEventCompleted,
        // que acá no se dispara porque la escena ya estaba cargada.
        NetworkObject jugador = nm.LocalClient != null ? nm.LocalClient.PlayerObject : null;
        if (jugador != null)
        {
            PlayerUI ui = jugador.GetComponentInChildren<PlayerUI>(true);
            if (ui != null && ui.hud != null) ui.hud.SetActive(true);
        }
    }
#endif
}