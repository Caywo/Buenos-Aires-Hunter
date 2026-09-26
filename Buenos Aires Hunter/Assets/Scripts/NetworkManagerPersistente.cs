using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

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

    public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeServicesAsync();
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
