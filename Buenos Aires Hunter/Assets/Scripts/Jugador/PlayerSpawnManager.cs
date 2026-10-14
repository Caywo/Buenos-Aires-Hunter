using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;

    private readonly HashSet<ulong> jugadoresProcesados = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarJugador;

        // También contempla jugadores que ya estaban conectados.
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                AlConectarJugador(clientId);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarJugador;
        }
    }

    private void AlConectarJugador(ulong clientId)
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
            return;

        if (!jugadoresProcesados.Add(clientId))
            return;

        StartCoroutine(EsperarJugadorYUbicarlo(clientId));
    }

    private IEnumerator EsperarJugadorYUbicarlo(ulong clientId)
    {
        float tiempoEspera = 5f;

        while (tiempoEspera > 0f)
        {
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsServer)
            {
                jugadoresProcesados.Remove(clientId);
                yield break;
            }

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                    clientId, out NetworkClient cliente))
            {
                jugadoresProcesados.Remove(clientId);
                yield break;
            }

            if (cliente.PlayerObject != null)
                break;

            tiempoEspera -= Time.deltaTime;
            yield return null;
        }

        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.ConnectedClients.TryGetValue(
                clientId, out NetworkClient jugadorCliente) ||
            jugadorCliente.PlayerObject == null)
        {
            jugadoresProcesados.Remove(clientId);
            yield break;
        }

        Transform punto = clientId == 0 ? spawnPoint1 : spawnPoint2;

        if (punto == null)
        {
            jugadoresProcesados.Remove(clientId);
            yield break;
        }

        NetworkObject jugador = jugadorCliente.PlayerObject;
        CharacterController controlador =
            jugador.GetComponent<CharacterController>();

        if (controlador != null)
            controlador.enabled = false;

        jugador.transform.SetPositionAndRotation(
            punto.position,
            punto.rotation
        );

        if (controlador != null)
            controlador.enabled = true;

        AplicarDatosGuardadoSiHay(clientId, jugador);
    }

    private void AplicarDatosGuardadoSiHay(
        ulong clientId, NetworkObject jugador)
    {
        if (SaveManager.Instance == null)
            return;

        SaveData save = SaveManager.Instance.CargarPartida();

        if (save == null || save.jugadores == null)
            return;

        int indice = clientId == 0 ? 0 : 1;

        if (indice >= save.jugadores.Length)
            return;

        PlayerInventory inventario =
            jugador.GetComponent<PlayerInventory>();

        inventario?.AplicarDatosGuardado(save.jugadores[indice]);
    }

    public Transform ObtenerPuntoRespawn(ulong clientId)
    {
        Transform porDefecto =
            clientId == 0 ? spawnPoint1 : spawnPoint2;

        if (NetworkManager.Singleton == null)
            return porDefecto;

        Transform[] puntos = { spawnPoint1, spawnPoint2 };
        Transform mejor = porDefecto;
        float mejorDistancia = -1f;
        bool hayOtros = false;

        foreach (Transform punto in puntos)
        {
            if (punto == null)
                continue;

            float distanciaMinima = float.MaxValue;

            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            {
                if (kv.Key == clientId || kv.Value.PlayerObject == null)
                    continue;

                Health salud =
                    kv.Value.PlayerObject.GetComponent<Health>();

                if (salud != null && salud.EstaMuerto)
                    continue;

                hayOtros = true;

                float distancia = Vector3.Distance(
                    punto.position,
                    kv.Value.PlayerObject.transform.position
                );

                distanciaMinima = Mathf.Min(
                    distanciaMinima, distancia);
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