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


/// Vive en el mismo GameObject que el NetworkManager y persiste entre escenas.
/// Inicializa Unity Gaming Services (Authentication) y expone los métodos para
/// crear/unirse a una partida usando Relay.

public class NetworkManagerPersistente : MonoBehaviour
{
    public static NetworkManagerPersistente Instance { get; private set; }

    /// Mensaje para que el menú muestre por qué volvió el jugador (se vacía al leerlo).
    public static string MensajeParaMenu;

    [Tooltip("Cantidad máxima de CLIENTES (sin contar al host). Ej: 1 = partidas de 2 jugadores.")]
    [SerializeField] private int maxConnections = 1;

    [Tooltip("Tipo de conexión de Relay. 'dtls' es lo recomendado (cifrado).")]
    [SerializeField] private string connectionType = "dtls";

    [Tooltip("Escena a la que se vuelve si se pierde la conexión con el host o con Relay.")]
    [SerializeField] private string escenaMenuPorDefecto = "MenuPrincipal";

    [Header("Tiempos de red")]
    [Tooltip("Milisegundos sin respuesta para dar por caída la conexión (por defecto de Unity: 30000).")]
    [SerializeField] private int disconnectTimeoutMs = 5000;
    [Tooltip("Intentos de conexión del cliente (por defecto de Unity: 60).")]
    [SerializeField] private int maxConnectAttempts = 5;

    private bool volviendoAlMenu;
    private bool apagadoIntencional; // evita que un apagado hecho por nosotros dispare "volver al menú"

    private Task initTask;

    public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    // Prefab de jugador configurado en el NetworkManager (se guarda al iniciar)
    private GameObject playerPrefabOriginal;
    public GameObject PlayerPrefabOriginal => playerPrefabOriginal;

    
    /// Llamar ANTES de StartHost. true: el host NO crea jugadores al conectarse; el modo de juego los crea
    /// cuando la escena de juego ya cargó en todos. false: comportamiento normal.
    
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

        // Que el host no se congele para el otro jugador al cambiar de ventana.
        Application.runInBackground = true;

        var nm = GetComponent<NetworkManager>();
        if (nm != null) playerPrefabOriginal = nm.NetworkConfig.PlayerPrefab;
    }

    private async void Start()
    {
        if (Instance != this) return;

        if (NetworkManager.Singleton != null)
        {
            // Relay descarta la allocation (host cerrado, inactividad...) o el host se va.
            NetworkManager.Singleton.OnTransportFailure += AlFallarElTransporte;
            NetworkManager.Singleton.OnClientStopped += AlDetenerseCliente;
        }

        await InitializeServicesAsync();
        SuscribirseACargaDeEscena();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnTransportFailure -= AlFallarElTransporte;
            NetworkManager.Singleton.OnClientStopped -= AlDetenerseCliente;
        }
    }

    // Si el host cierra la ventana sin pasar por el botón de salir, avisar a los clientes.
    private void OnApplicationQuit()
    {
        apagadoIntencional = true;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    // ESCENAS 

    private void AlCargarEscena(string sceneName, LoadSceneMode mode,
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

    public void SuscribirseACargaDeEscena()
    {
        StartCoroutine(SuscribirseACargaDeEscenaCuandoEsteListo());
    }

    private IEnumerator SuscribirseACargaDeEscenaCuandoEsteListo()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= AlCargarEscena;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += AlCargarEscena;
    }

    
    /// Apaga la red, espera a que el apagado termine y recién entonces carga la escena
    /// con el SceneManager de Unity. Se ejecuta acá porque este objeto persiste entre escenas.
    
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
            apagadoIntencional = true;
            nm.Shutdown();

            while (nm != null && nm.ShutdownInProgress)
                yield return null;

            apagadoIntencional = false;
        }

        // Si el modo anterior dejó el Player Prefab en null (duelo), restaurarlo para la próxima partida.
        ConfigurarSpawnDeJugadores(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(escenaMenu, LoadSceneMode.Single);

        yield return null;
        volviendoAlMenu = false;
    }

    // CAÍDAS DE CONEXIÓN 

    // Unity Transport avisa cuando la conexión con Relay deja de ser válida.
    private void AlFallarElTransporte()
    {
        Debug.LogWarning("[Relay] Se perdió la conexión con el servidor de Relay. Volviendo al menú.");
        MensajeParaMenu = "Se perdió la conexión con el servidor.";
        VolverAlMenu(escenaMenuPorDefecto);
    }

    // Cliente: el host cerró la partida (o dejó de responder). Sin esto, el jugador 2 se queda
    // en la escena de juego sin jugador ni cámara, y parece congelado.
    private void AlDetenerseCliente(bool eraHost)
    {
        if (eraHost || volviendoAlMenu || apagadoIntencional)
            return;

        // Si falló un intento de unirse desde el menú, no recargar el menú.
        if (SceneManager.GetActiveScene().name == escenaMenuPorDefecto)
            return;

        Debug.LogWarning("[Red] Se perdió la conexión con el host. Volviendo al menú.");
        MensajeParaMenu = "El host abandonó la partida.";
        VolverAlMenu(escenaMenuPorDefecto);
    }

    // SERVICIOS (UGS) 

    
    /// Inicializa UGS y hace login anónimo si todavía no hay sesión.
    /// Seguro de llamar varias veces, incluso a la vez: todas comparten la misma tarea.
    
    public Task InitializeServicesAsync()
    {
        if (initTask == null || initTask.IsFaulted || (initTask.IsCompleted && !IsSignedIn))
            initTask = InicializarServiciosInterno();

        return initTask;
    }

    private async Task InicializarServiciosInterno()
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

    // RELAY 

    private async Task AsegurarRedApagadaAsync()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm.IsListening || nm.ShutdownInProgress)
        {
            apagadoIntencional = true;
            nm.Shutdown();

            while (nm.ShutdownInProgress)
                await Task.Yield();

            apagadoIntencional = false;
        }
    }

    
    /// Crea una allocation de Relay, configura el transporte y arranca el host.
    /// Devuelve el join code para compartir con los clientes, o null si falló.
    
    public async Task<string> StartHostWithRelayAsync()
    {
        await InitializeServicesAsync();

        if (!IsSignedIn)
        {
            Debug.LogError("[Relay] No hay sesión de Authentication activa todavía.");
            return null;
        }

        try
        {
            await AsegurarRedApagadaAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            RelayServerData relayServerData = allocation.ToRelayServerData(connectionType);
            transport.SetRelayServerData(relayServerData);

            // Detectar rápido que un cliente se fue.
            transport.DisconnectTimeoutMS = disconnectTimeoutMs;

            SuscribirseACargaDeEscena();

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

    
    /// Se une a una allocation de Relay existente usando el join code y arranca el cliente.
    
    public async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        await InitializeServicesAsync();

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
            await AsegurarRedApagadaAsync();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            RelayServerData relayServerData = joinAllocation.ToRelayServerData(connectionType);
            transport.SetRelayServerData(relayServerData);

            // Si la partida está llena o Relay no responde, fallar en segundos y no en un minuto.
            transport.DisconnectTimeoutMS = disconnectTimeoutMs;
            transport.MaxConnectAttempts = maxConnectAttempts;
            transport.ConnectTimeoutMS = 1000;

            SuscribirseACargaDeEscena();

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