using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinijuegoPPTUI : MonoBehaviour
{
    [Header("Pantalla")]
    [SerializeField] private GameObject panel;

    [Header("Botones")]
    [SerializeField] private Button botonPiedra;
    [SerializeField] private Button botonPapel;
    [SerializeField] private Button botonTijera;
    [Tooltip("Cierra el juego y vuelve a la tienda.")]
    [SerializeField] private Button botonCerrar;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoPuntaje;
    [SerializeField] private TMP_Text textoJugadas;
    [SerializeField] private TMP_Text textoMensaje;

    private static readonly string[] Nombres = { "Piedra", "Papel", "Tijera" };

    private MinijuegoPiedraPapelTijera juego;
    private PlayerInventory jugadorActual;
    private TiendaUI tiendaOrigen;
    private bool esperando;
    private bool terminada;

    public bool EstaAbierta => panel.activeSelf;

    private void Awake()
    {
        botonPiedra.onClick.AddListener(() => Elegir(Jugada.Piedra));
        botonPapel.onClick.AddListener(() => Elegir(Jugada.Papel));
        botonTijera.onClick.AddListener(() => Elegir(Jugada.Tijera));
        botonCerrar.onClick.AddListener(Cerrar);

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        Desuscribir();
    }

    /// <summary>La llama la tienda al apretar el botón "De una".</summary>
    public void Abrir(MinijuegoPiedraPapelTijera nuevoJuego, PlayerInventory jugador, TiendaUI tienda)
    {
        gameObject.SetActive(true);
        jugadorActual = jugador;
        tiendaOrigen = tienda;

        if (juego != nuevoJuego)
        {
            Desuscribir();
            juego = nuevoJuego;
            juego.OnInicio += AlIniciar;
            juego.OnRonda += AlRecibirRonda;
        }

        panel.SetActive(true);
        BloquearControles(true);

        textoMensaje.text = "Conectando con Horacio...";
        textoJugadas.text = string.Empty;
        textoPuntaje.text = string.Empty;
        PonerBotones(false);

        juego.Iniciar();
    }

    public void Cerrar()
    {
        panel.SetActive(false);
        BloquearControles(false);

        // Vuelve a la tienda.
        if (tiendaOrigen != null && jugadorActual != null)
        {
            TiendaUI tienda = tiendaOrigen;
            tiendaOrigen = null;
            tienda.AbrirTienda(jugadorActual);
        }
    }

    // Igual que la tienda: bloquea movimiento, cámara e inventario y libera el cursor.
    private void BloquearControles(bool bloquear)
    {
        if (jugadorActual != null)
        {
            jugadorActual.GetComponent<PlayerLook>().inputBloqueado = bloquear;
            jugadorActual.inputBloqueado = bloquear;
            jugadorActual.GetComponent<PlayerMotor>().inputBloqueado = bloquear;
        }

        Cursor.lockState = bloquear ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = bloquear;
    }

    private void Desuscribir()
    {
        if (juego == null)
            return;

        juego.OnInicio -= AlIniciar;
        juego.OnRonda -= AlRecibirRonda;
        juego = null;
    }

    private void Elegir(Jugada jugada)
    {
        if (esperando || terminada)
            return;

        esperando = true;
        PonerBotones(false);
        juego.Jugar(jugada);
    }

    private void PonerBotones(bool activos)
    {
        botonPiedra.interactable = activos;
        botonPapel.interactable = activos;
        botonTijera.interactable = activos;
    }

    private void MostrarPuntaje(int jugador, int vendedor)
    {
        textoPuntaje.text = $"Vos {jugador} - {vendedor} Horacio";
    }

    private void AlIniciar(int puntosJugador, int puntosVendedor)
    {
        esperando = false;
        terminada = false;

        MostrarPuntaje(puntosJugador, puntosVendedor);
        textoMensaje.text = $"El primero en ganar {juego.VictoriasNecesarias} rondas gana. ¡Elegí!";
        PonerBotones(true);
    }

    private void AlRecibirRonda(int jugadaJugador, int jugadaVendedor, int resultado,
                                int puntosJugador, int puntosVendedor, bool termino)
    {
        esperando = false;

        MostrarPuntaje(puntosJugador, puntosVendedor);
        textoJugadas.text = $"Elegiste {Nombres[jugadaJugador]}. Horacio eligió {Nombres[jugadaVendedor]}.";

        if (!termino)
        {
            textoMensaje.text = resultado == 1 ? "¡Ganaste la ronda!"
                              : resultado == -1 ? "Perdiste la ronda."
                              : "Empate, se repite.";
            PonerBotones(true);
            return;
        }

        terminada = true;
        PonerBotones(false);

        textoMensaje.text = puntosJugador > puntosVendedor
            ? "¡Ganaste la partida!"
            : "Perdiste la partida.";
    }
}