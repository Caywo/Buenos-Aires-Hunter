using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarJugador;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

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

    /// <summary>
    /// SOLO SERVIDOR. Devuelve el punto de respawn más lejano a los demás jugadores vivos
    /// (en un duelo evita reaparecer encima del rival). Si no hay otros, usa el punto "propio".
    /// </summary>
    public Transform ObtenerPuntoRespawn(ulong clientId)
    {
        Transform porDefecto = clientId == 0 ? spawnPoint1 : spawnPoint2;
        if (NetworkManager.Singleton == null) return porDefecto;

        Transform[] puntos = { spawnPoint1, spawnPoint2 };
        Transform mejor = porDefecto;
        float mejorDistancia = -1f;
        bool hayOtros = false;

        foreach (Transform punto in puntos)
        {
            if (punto == null) continue;

            float distanciaMinima = float.MaxValue;
            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            {
                if (kv.Key == clientId || kv.Value.PlayerObject == null) continue;

                Health h = kv.Value.PlayerObject.GetComponent<Health>();
                if (h != null && h.EstaMuerto) continue;

                hayOtros = true;
                float d = Vector3.Distance(punto.position, kv.Value.PlayerObject.transform.position);
                distanciaMinima = Mathf.Min(distanciaMinima, d);
            }

            if (distanciaMinima > mejorDistancia)
            {
                mejorDistancia = distanciaMinima;
                mejor = punto;
            }
        }

        return hayOtros ? mejor : porDefecto;
    }
}