using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Vive en el mismo GameObject que el NetworkManager y persiste entre escenas.
/// Además de eso, ahora inicializa Unity Gaming Services (Authentication)
/// y expone los métodos para crear/unirse a una partida usando Relay.
/// </summary>
public class NetworkManagerPersistente : MonoBehaviour
{
    public static NetworkManagerPersistente Instance { get; private set; }

    [Tooltip("Cantidad máxima de CLIENTES (sin contar al host). Ej: 1 = partidas de 2 jugadores.")]
    [SerializeField] private int maxConnections = 1;

    [Tooltip("Tipo de conexión de Relay. 'dtls' es lo recomendado (cifrado).")]
    [SerializeField] private string connectionType = "dtls";

    [Tooltip("Escena a la que se vuelve si se pierde la conexión con el servidor de Relay.")]
    [SerializeField] private string escenaMenuPorDefecto = "MenuPrincipal";

    private bool volviendoAlMenu;

    public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    // NUEVO: prefab de jugador configurado en el NetworkManager (se guarda al iniciar)
    private GameObject playerPrefabOriginal;
    public GameObject PlayerPrefabOriginal => playerPrefabOriginal;
    /// <summary>
    /// NUEVO. Llamar ANTES de StartHost. true: el host NO crea jugadores al conectarse; el modo de juego los crea
    /// cuando la escena de juego ya cargó en todos (evita mover jugadores entre escenas). false: comportamiento normal.
    /// </summary>
    public void ConfigurarSpawnDeJugadores(bool alCargarEscena)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.NetworkConfig.PlayerPrefab = alCargarEscena ? null : playerPrefabOriginal;

        Debug.Log($"[Spawn] Crear jugadores al cargar la escena: {alCargarEscena}. " +
                  $"Player Prefab del host: {(nm.NetworkConfig.PlayerPrefab != null ? nm.NetworkConfig.PlayerPrefab.name : "ninguno")}. " +
                  $"Prefab guardado: {(playerPrefabOriginal != null ? playerPrefabOriginal.name : "NINGUNO")}");
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var nm = GetComponent<NetworkManager>();
        if (nm != null) playerPrefabOriginal = nm.NetworkConfig.PlayerPrefab;

    }
    private void AlCargarEscena(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode,
        System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton.LocalClient == null) return;

        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject == null) return;

        var playerUI = playerObject.GetComponentInChildren<PlayerUI>(true);
        if (playerUI != null && playerUI.hud != null)
        {
            playerUI.hud.SetActive(true);
        }
    }
    private async void Start()
    {
        // NUEVO: si Relay descarta la allocation (host cerrado, inactividad...), volver al menú en vez de quedar a medias
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnTransportFailure += AlFallarElTransporte;

        await InitializeServicesAsync();
        StartCoroutine(SuscribirseACargaDeEscenaCuandoEsteListo());

    }
    private IEnumerator SuscribirseACargaDeEscenaCuandoEsteListo()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += AlCargarEscena;
    }
    /// <summary>
    /// NUEVO. Apaga la red, espera a que el apagado termine y recién entonces carga la escena
    /// con el SceneManager de Unity. Hacer Shutdown() justo después del LoadScene de red
    /// cancelaba la carga. Se ejecuta acá porque este objeto persiste entre escenas.
    /// </summary>
    public void VolverAlMenu(string escenaMenu)
    {
        if (volviendoAlMenu) return; // ya se está yendo: evita cargar el menú dos veces
        volviendoAlMenu = true;
        StartCoroutine(VolverAlMenuRoutine(escenaMenu));
    }

    private IEnumerator VolverAlMenuRoutine(string escenaMenu)
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null)
        {
            nm.Shutdown();
            while (nm != null && nm.ShutdownInProgress)
                yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(escenaMenu, LoadSceneMode.Single);

        yield return null;
        volviendoAlMenu = false;
    }

    // NUEVO: Unity Transport avisa cuando la conexión con Relay deja de ser válida
    // ("allocation ID not found"). Netcode ya apaga la red; acá solo se vuelve al menú.
    private void AlFallarElTransporte()
    {
        Debug.LogWarning("[Relay] Se perdió la conexión con el servidor de Relay (la allocation dejó de ser válida). Volviendo al menú.");
        VolverAlMenu(escenaMenuPorDefecto);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnTransportFailure -= AlFallarElTransporte;
    }

    /// <summary>
    /// Inicializa UGS y hace login anónimo si todavía no hay sesión. Seguro de llamar varias veces.
    /// </summary>
    public async Task InitializeServicesAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignedIn += () =>
                    Debug.Log($"[UGS] Sesión iniciada. PlayerId: {AuthenticationService.Instance.PlayerId}");

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error inicializando servicios: {e.Message}");
        }
    }

    /// <summary>
    /// Crea una allocation de Relay, configura el transporte y arranca el host.
    /// Devuelve el join code para compartir con los clientes, o null si falló.
    /// </summary>
    public async Task<string> StartHostWithRelayAsync()
    {
        if (!IsSignedIn)
        {
            Debug.LogError("[Relay] No hay sesión de Authentication activa todavía.");
            return null;
        }

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // El constructor directo "new RelayServerData(allocation, connectionType)" ya no existe
            // en las versiones nuevas de Unity Transport / Multiplayer Services: ahora se usa este
            // método de extensión (Unity.Services.Relay.Models.AllocationUtils).
            RelayServerData relayServerData = allocation.ToRelayServerData(connectionType);
            transport.SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                Debug.LogError("[Relay] StartHost() devolvió false.");
                return null;
            }

            Debug.Log($"[Relay] Host iniciado. Join code: {joinCode}");
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Relay] Error creando la partida: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Se une a una allocation de Relay existente usando el join code y arranca el cliente.
    /// </summary>
    public async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        if (!IsSignedIn)
        {
            Debug.LogError("[Relay] No hay sesión de Authentication activa todavía.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogError("[Relay] El join code está vacío.");
            return false;
        }

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            RelayServerData relayServerData = joinAllocation.ToRelayServerData(connectionType);
            transport.SetRelayServerData(relayServerData);

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                Debug.LogError("[Relay] StartClient() devolvió false.");
            }

            return started;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Relay] Error uniéndose a la partida (código '{joinCode}'): {e.Message}");
            return false;
        }
    }
}
