
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    [Header("Puntos de aparición")]
    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;

    private readonly HashSet<ulong> jugadoresProcesados = new();
    private bool posicionandoJugadores;

    private NetworkManager networkManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback += AlConectarJugador;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted +=
                AlTerminarCargaEscena;
        }

        if (networkManager.IsServer)
        {
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
                AlConectarJugador(clientId);
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= AlConectarJugador;

            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoadEventCompleted -=
                    AlTerminarCargaEscena;
            }
        }

        if (Instance == this)
            Instance = null;
    }

    private void AlConectarJugador(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        StartCoroutine(EsperarJugadorYUbicarlo(clientId));
    }

    private void AlTerminarCargaEscena(
        string nombreEscena,
        LoadSceneMode modo,
        List<ulong> clientesCompletados,
        List<ulong> clientesFallidos)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        // Cada carga de escena requiere ubicar a los jugadores de nuevo.
        jugadoresProcesados.Clear();

        if (!posicionandoJugadores)
            StartCoroutine(PosicionarTodosLosJugadores());
    }

    private IEnumerator PosicionarTodosLosJugadores()
    {
        posicionandoJugadores = true;

        // Esperar a que los objetos de jugador estén disponibles.
        yield return null;
        yield return new WaitForEndOfFrame();

        if (networkManager == null || !networkManager.IsServer)
        {
            posicionandoJugadores = false;
            yield break;
        }

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            yield return StartCoroutine(
                EsperarJugadorYUbicarlo(clientId)
            );
        }

        posicionandoJugadores = false;
    }

    private IEnumerator EsperarJugadorYUbicarlo(ulong clientId)
    {
        float tiempoEspera = 5f;

        while (tiempoEspera > 0f)
        {
            if (networkManager == null || !networkManager.IsServer)
                yield break;

            if (!networkManager.ConnectedClients.TryGetValue(
                    clientId, out NetworkClient cliente))
                yield break;

            if (cliente.PlayerObject != null)
                break;

            tiempoEspera -= Time.deltaTime;
            yield return null;
        }

        if (networkManager == null ||
            !networkManager.IsServer ||
            !networkManager.ConnectedClients.TryGetValue(
                clientId, out NetworkClient jugadorCliente) ||
            jugadorCliente.PlayerObject == null)
            yield break;

        // Evitar procesar dos veces al mismo jugador en una misma carga.
        if (!jugadoresProcesados.Add(clientId))
            yield break;

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

        // Sincronizar el teletransporte si el jugador utiliza NetworkTransform.
        NetworkTransform networkTransform =
            jugador.GetComponent<NetworkTransform>();

        if (networkTransform != null)
            networkTransform.Teleport(
                punto.position,
                punto.rotation,
                jugador.transform.localScale
            );

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

        if (networkManager == null)
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

            foreach (var kv in networkManager.ConnectedClients)
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
                    distanciaMinima, distancia
                );
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
