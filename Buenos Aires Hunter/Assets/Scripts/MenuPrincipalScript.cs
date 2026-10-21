using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [System.Serializable]
    public class ModoDeJuego
    {
        public string nombre;
        public GameObject panel;
        public string escenaDeJuego;

        [Header("Multijugador de este modo (opcional)")]
        [Tooltip("Campo donde se escribe el código en el panel de ESTE modo. Si queda vacío se usa 'Join Code Input'.")]
        public TMP_InputField joinCodeInput;
        [Tooltip("Texto donde se muestra el código y la espera en el panel de ESTE modo. Si queda vacío se usa 'Join Code Display'.")]
        public TMP_Text joinCodeDisplay;

        [Header("Jugadores")]
        [Tooltip("Si está tildado, los jugadores NO se crean en el menú: el modo los crea al terminar de cargar la escena (lo usa el duelo).")]
        public bool crearJugadoresAlCargarEscena;
    }

    [Header("Modos de juego")]
    [SerializeField] private ModoDeJuego[] modos;

    [Tooltip("Cantidad de jugadores necesarios para arrancar la partida (host incluido).")]
    [SerializeField] private int jugadoresNecesarios = 2;

    [Header("Multijugador (Relay)")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;

    private ModoDeJuego ObtenerModoActual(bool avisar = true)
    {
        foreach (var modo in modos)
        {
            if (modo.panel != null && modo.panel.activeInHierarchy)
                return modo;
        }

        if (avisar) Debug.LogError("No hay ningún panel de modo activo.");
        return null;
    }

    // Usa el campo propio del modo; si no tiene, el general (así los modos existentes siguen igual)
    private TMP_InputField InputDe(ModoDeJuego modo)
    {
        return (modo != null && modo.joinCodeInput != null) ? modo.joinCodeInput : joinCodeInput;
    }

    private TMP_Text DisplayDe(ModoDeJuego modo)
    {
        return (modo != null && modo.joinCodeDisplay != null) ? modo.joinCodeDisplay : joinCodeDisplay;
    }

    public void UnJugador()
    {
        var modo = ObtenerModoActual();
        if (modo == null) return;

        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData("127.0.0.1", 7777);

        NetworkManagerPersistente.Instance.ConfigurarSpawnDeJugadores(false);
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene(
            modo.escenaDeJuego,
            LoadSceneMode.Single
        );
    }

    public async void CrearPartida()
    {
        modoElegido = ObtenerModoActual();
        if (modoElegido == null) return;

        NetworkManagerPersistente.Instance.ConfigurarSpawnDeJugadores(modoElegido.crearJugadoresAlCargarEscena);

        string joinCode = await NetworkManagerPersistente.Instance.StartHostWithRelayAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("No se pudo crear la partida.");
            return;
        }

        MostrarCodigoYEsperando(joinCode);

        EsperarJugadores();
    }

    public async void UnirseAPartida()
    {
        TMP_InputField input = InputDe(ObtenerModoActual(false));

        if (input == null || string.IsNullOrWhiteSpace(input.text))
        {
            Debug.LogError("Ingresá un join code antes de unirte.");
            return;
        }

        bool conectado = await NetworkManagerPersistente.Instance.StartClientWithRelayAsync(input.text);

        if (!conectado)
        {
            Debug.LogError("No se pudo unir a la partida. Revisá el código.");
        }
    }

    // ---------- Continuar partida guardada ----------

    public void ContinuarPartidaUnJugador()
    {
        if (!CargarPartida()) return;

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7777);

        NetworkManagerPersistente.Instance.ConfigurarSpawnDeJugadores(false);
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene(
            modoElegido.escenaDeJuego,
            LoadSceneMode.Single
        );
    }

    public async void ContinuarPartidaOnline()
    {
        if (!CargarPartida()) return;

        NetworkManagerPersistente.Instance.ConfigurarSpawnDeJugadores(false);

        string joinCode = await NetworkManagerPersistente.Instance.StartHostWithRelayAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("No se pudo crear la partida.");
            return;
        }

        MostrarCodigoYEsperando(joinCode);

        EsperarJugadores();
    }

    private bool CargarPartida()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HayPartidaGuardada())
        {
            Debug.LogError("No hay partida guardada.");
            return false;
        }

        var save = SaveManager.Instance.CargarPartida();

        modoElegido = new ModoDeJuego
        {
            nombre = "Continuar",
            panel = null,
            escenaDeJuego = save.escenaSiguiente
        };

        return true;
    }

    // ---------------------------------------------------

    private ModoDeJuego modoElegido;

    // Jugadores remotos que YA terminaron de sincronizar la escena actual (el menú)
    private readonly HashSet<ulong> clientesSincronizados = new HashSet<ulong>();

    // Escucha quién se conecta (para el texto de espera) y quién terminó de sincronizar (para arrancar)
    private void EsperarJugadores()
    {
        clientesSincronizados.Clear();

        NetworkManager.Singleton.OnClientConnectedCallback += OnJugadorConectado;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnJugadorSincronizado;

        RevisarSiHayQueArrancar();
    }

    private void DejarDeEsperar()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnJugadorConectado;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnJugadorSincronizado;
    }

    // Conectado NO significa listo: solo actualiza el texto de espera
    private void OnJugadorConectado(ulong clientId)
    {
        RevisarSiHayQueArrancar();
    }

    // El cliente terminó de sincronizar el menú: recién ahora es seguro cambiar de escena
    private void OnJugadorSincronizado(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            clientesSincronizados.Add(clientId);

        RevisarSiHayQueArrancar();
    }

    private void RevisarSiHayQueArrancar()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (modoElegido == null) return;

        int conectados = NetworkManager.Singleton.ConnectedClientsList.Count;

        // El host cuenta siempre; los demás, solo cuando ya sincronizaron y siguen conectados
        int listos = 1;
        foreach (ulong id in clientesSincronizados)
        {
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(id)) listos++;
        }

        if (listos >= jugadoresNecesarios && (modoElegido.crearJugadoresAlCargarEscena || TodosTienenJugador()))
        {
            DejarDeEsperar();

            NetworkManager.Singleton.SceneManager.LoadScene(
                modoElegido.escenaDeJuego,
                LoadSceneMode.Single
            );
            return;
        }

        TMP_Text display = DisplayDe(modoElegido);
        if (display == null) return;

        if (conectados >= jugadoresNecesarios)
            display.text = $"Código: {ultimoJoinCode} | Jugador conectado, sincronizando...";
        else
            display.text = $"Código: {ultimoJoinCode} | Esperando jugador... ({conectados}/{jugadoresNecesarios})";
    }

    private bool TodosTienenJugador()
    {
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject == null) return false;
        }
        return true;
    }

    private string ultimoJoinCode;

    private void MostrarCodigoYEsperando(string joinCode)
    {
        ultimoJoinCode = joinCode;

        TMP_Text display = DisplayDe(modoElegido);
        if (display != null)
        {
            display.text = $"Código: {joinCode}\nEsperando jugador... (1/{jugadoresNecesarios})";
        }
    }

    private void OnDestroy()
    {
        DejarDeEsperar();
    }
}