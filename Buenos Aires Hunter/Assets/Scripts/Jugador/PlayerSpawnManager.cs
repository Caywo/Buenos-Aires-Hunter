using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarJugador;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarJugador;
        }
    }

    private void AlConectarJugador(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient cliente))
            return;

        if (cliente.PlayerObject == null)
            return;

        if (clientId == 0)
        {
            cliente.PlayerObject.transform.position = spawnPoint1.position;
        }
        else
        {
            cliente.PlayerObject.transform.position = spawnPoint2.position;
        }
    }
}