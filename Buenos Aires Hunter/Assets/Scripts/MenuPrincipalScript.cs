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
    }

    [Header("Modos de juego")]
    [SerializeField] private ModoDeJuego[] modos;

    [Tooltip("Cantidad de jugadores necesarios para arrancar la partida (host incluido).")]
    [SerializeField] private int jugadoresNecesarios = 2;

    [Header("Multijugador (Relay)")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;

    private ModoDeJuego ObtenerModoActual()
    {
        foreach (var modo in modos)
        {
            Debug.Log(modo.nombre + " -> panel=" + (modo.panel != null ? modo.panel.name : "NULL") +
                       " activeInHierarchy=" + (modo.panel != null && modo.panel.activeInHierarchy));
        }

        foreach (var modo in modos)
        {
            if (modo.panel != null && modo.panel.activeInHierarchy)
                return modo;
        }

        Debug.LogError("No hay ningún panel de modo activo.");
        return null;
    }

    public void UnJugador()
    {
        var modo = ObtenerModoActual();
        if (modo == null) return;

        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData("127.0.0.1", 7777);

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

        string joinCode = await NetworkManagerPersistente.Instance.StartHostWithRelayAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("No se pudo crear la partida.");
            return;
        }

        MostrarCodigoYEsperando(joinCode);

        NetworkManager.Singleton.OnClientConnectedCallback += OnJugadorConectado;
        RevisarSiHayQueArrancar();
    }

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
    }

    private ModoDeJuego modoElegido;

    private void OnJugadorConectado(ulong clientId)
    {
        RevisarSiHayQueArrancar();
    }

    private void RevisarSiHayQueArrancar()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        if (modoElegido == null) return;

        int conectados = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (conectados >= jugadoresNecesarios)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnJugadorConectado;

            NetworkManager.Singleton.SceneManager.LoadScene(
                modoElegido.escenaDeJuego,
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