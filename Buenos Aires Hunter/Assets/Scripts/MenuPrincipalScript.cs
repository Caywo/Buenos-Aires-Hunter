using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Multijugador (Relay)")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;
    [SerializeField] private string escenaDeJuego = "Escenario1";

    [Tooltip("Cantidad de jugadores necesarios para arrancar la partida (host incluido).")]
    [SerializeField] private int jugadoresNecesarios = 2;

    /// <summary>
    /// Prueba local sin Relay/Authentication (host directo por IP). Útil para testear solo.
    /// </summary>
    public void UnJugador()
    {
        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData("127.0.0.1", 7777);

        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Escenario1",
            LoadSceneMode.Single
        );
    }

    /// <summary>
    /// Crea una partida usando Relay y muestra el join code en pantalla.
    /// NO carga la escena todavía: espera a que se conecten los jugadores
    /// necesarios (ver OnClientConnectedCallback).
    /// </summary>
    public async void CrearPartida()
    {
        string joinCode = await NetworkManagerPersistente.Instance.StartHostWithRelayAsync();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("No se pudo crear la partida.");
            return;
        }

        MostrarCodigoYEsperando(joinCode);

        // Puede que el host ya cumpla la condición (por ejemplo, si jugadoresNecesarios = 1),
        // así que chequeamos una vez de entrada además de escuchar futuras conexiones.
        NetworkManager.Singleton.OnClientConnectedCallback += OnJugadorConectado;
        RevisarSiHayQueArrancar();
    }

    /// <summary>
    /// Se une a una partida existente con el join code ingresado en el input field.
    /// </summary>
    public async void UnirseAPartida()
    {
        if (joinCodeInput == null || string.IsNullOrWhiteSpace(joinCodeInput.text))
        {
            Debug.LogError("Ingresá un join code antes de unirte.");
            return;
        }

        bool conectado = await NetworkManagerPersistente.Instance.StartClientWithRelayAsync(joinCodeInput.text);

        if (!conectado)
        {
            Debug.LogError("No se pudo unir a la partida. Revisá el código.");
        }

        // La carga de escena la dispara el HOST cuando ya están todos los jugadores
        // necesarios; NetworkManager.SceneManager la sincroniza sola a este cliente.
    }

    private void OnJugadorConectado(ulong clientId)
    {
        RevisarSiHayQueArrancar();
    }

    private void RevisarSiHayQueArrancar()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int conectados = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (conectados >= jugadoresNecesarios)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnJugadorConectado;

            // El host es quien dispara la carga de escena; NetworkManager.SceneManager
            // la sincroniza con todos los clientes conectados.
            NetworkManager.Singleton.SceneManager.LoadScene(
                escenaDeJuego,
                LoadSceneMode.Single
            );
        }
        else if (joinCodeDisplay != null)
        {
            joinCodeDisplay.text = $"Código: {ultimoJoinCode} | Esperando jugador... ({conectados}/{jugadoresNecesarios})";
        }
    }

    private string ultimoJoinCode;

    private void MostrarCodigoYEsperando(string joinCode)
    {
        ultimoJoinCode = joinCode;

        if (joinCodeDisplay != null)
        {
            joinCodeDisplay.text = $"Código: {joinCode}\nEsperando jugador... (1/{jugadoresNecesarios})";
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnJugadorConectado;
        }
    }
}
