using System.Collections;
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

    [Header("Elecciones (imágenes)")]
    [Tooltip("Muestra lo que elegiste vos.")]
    [SerializeField] private Image imagenJugador;
    [Tooltip("Muestra lo que eligió Horacio.")]
    [SerializeField] private Image imagenVendedor;
    [SerializeField] private Sprite spritePiedra;
    [SerializeField] private Sprite spritePapel;
    [SerializeField] private Sprite spriteTijera;
    [Tooltip("Imagen antes de revelar (puño cerrado o '?'). Opcional.")]
    [SerializeField] private Sprite spriteOculto;
    [Tooltip("Espeja la imagen de Horacio para que las manos se miren de frente.")]
    [SerializeField] private bool espejarVendedor = false;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoPuntaje;
    [SerializeField] private TMP_Text textoMensaje;
    [Tooltip("Nombre de la jugada debajo de cada imagen. Opcional.")]
    [SerializeField] private TMP_Text nombreJugador;
    [SerializeField] private TMP_Text nombreVendedor;

    [Header("Animación")]
    [Tooltip("Tiempo mínimo que Horacio 'piensa' antes de revelar su jugada.")]
    [SerializeField] private float duracionSuspenso = 0.9f;
    [Tooltip("Pausa después de cada ronda antes de poder elegir de nuevo.")]
    [SerializeField] private float pausaEntreRondas = 1.3f;
    [SerializeField] private Color colorGana = new Color(0.45f, 1f, 0.45f);
    [SerializeField] private Color colorPierde = new Color(1f, 0.45f, 0.45f);
    [SerializeField] private Color colorEmpate = Color.white;

    private static readonly string[] Nombres = { "Piedra", "Papel", "Tijera" };

    private MinijuegoPiedraPapelTijera juego;
    private PlayerInventory jugadorActual;
    private TiendaUI tiendaOrigen;
    private bool esperando;
    private bool terminada;
    private float inicioEspera;
    private Coroutine rutinaTemblor;
    private Coroutine rutinaRonda;

    public bool EstaAbierta => panel.activeSelf;

    private void Awake()
    {
        botonPiedra.onClick.AddListener(() => Elegir(Jugada.Piedra));
        botonPapel.onClick.AddListener(() => Elegir(Jugada.Papel));
        botonTijera.onClick.AddListener(() => Elegir(Jugada.Tijera));
        botonCerrar.onClick.AddListener(Cerrar);

        if (imagenJugador != null) imagenJugador.preserveAspect = true;
        if (imagenVendedor != null) imagenVendedor.preserveAspect = true;

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

        DetenerAnimaciones();
        ReiniciarImagenes();

        textoMensaje.text = "Conectando con Horacio...";
        textoPuntaje.text = string.Empty;
        PonerBotones(false);

        juego.Iniciar();
    }

    public void Cerrar()
    {
        DetenerAnimaciones();

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

    // ───────────────────────── JUEGO ─────────────────────────

    private void Elegir(Jugada jugada)
    {
        if (esperando || terminada)
            return;

        esperando = true;
        inicioEspera = Time.time;
        PonerBotones(false);

        // Tu elección se ve enseguida; la de Horacio queda temblando hasta revelarse.
        ReiniciarImagenes();
        MostrarEleccion(imagenJugador, nombreJugador, (int)jugada);
        PonerOculto(imagenVendedor, nombreVendedor, "...");

        textoMensaje.text = string.Empty;

        if (imagenVendedor != null)
            rutinaTemblor = StartCoroutine(Temblar(imagenVendedor.rectTransform));

        juego.Jugar(jugada);
    }

    private void AlIniciar(int puntosJugador, int puntosVendedor)
    {
        esperando = false;
        terminada = false;

        DetenerAnimaciones();
        ReiniciarImagenes();

        MostrarPuntaje(puntosJugador, puntosVendedor);
        textoMensaje.text = $"El primero en ganar {juego.VictoriasNecesarias} rondas gana. ¡Elegí!";
        PonerBotones(true);
    }

    private void AlRecibirRonda(int jugadaJugador, int jugadaVendedor, int resultado,
                                int puntosJugador, int puntosVendedor, bool termino)
    {
        if (rutinaRonda != null)
            StopCoroutine(rutinaRonda);

        rutinaRonda = StartCoroutine(RevelarRonda(jugadaJugador, jugadaVendedor, resultado,
                                                  puntosJugador, puntosVendedor, termino));
    }

    private IEnumerator RevelarRonda(int jugadaJugador, int jugadaVendedor, int resultado,
                                     int puntosJugador, int puntosVendedor, bool termino)
    {
        // Suspenso mínimo: Horacio "piensa" un momento aunque la respuesta llegue al instante.
        float restante = duracionSuspenso - (Time.time - inicioEspera);

        if (restante > 0f)
            yield return new WaitForSeconds(restante);

        DetenerTemblor();

        // Revelación
        MostrarEleccion(imagenJugador, nombreJugador, jugadaJugador);
        MostrarEleccion(imagenVendedor, nombreVendedor, jugadaVendedor);

        if (imagenVendedor != null)
            yield return StartCoroutine(Pop(imagenVendedor.rectTransform));

        // Resultado: verde gana, rojo pierde, blanco empata.
        if (imagenJugador != null)
            imagenJugador.color = resultado == 1 ? colorGana : resultado == -1 ? colorPierde : colorEmpate;

        if (imagenVendedor != null)
            imagenVendedor.color = resultado == -1 ? colorGana : resultado == 1 ? colorPierde : colorEmpate;

        MostrarPuntaje(puntosJugador, puntosVendedor);

        if (termino)
        {
            terminada = true;
            esperando = false;
            PonerBotones(false);

            textoMensaje.text = puntosJugador > puntosVendedor
                ? "¡Ganaste la partida!"
                : "Perdiste la partida.";

            rutinaRonda = null;
            yield break;
        }

        textoMensaje.text = resultado == 1 ? "¡Ganaste la ronda!"
                          : resultado == -1 ? "Perdiste la ronda."
                          : "Empate, se repite.";

        yield return new WaitForSeconds(pausaEntreRondas);

        ReiniciarImagenes();
        textoMensaje.text = "¡Elegí!";
        esperando = false;
        PonerBotones(true);
        rutinaRonda = null;
    }

    // ───────────────────────── VISUAL ─────────────────────────

    private Sprite SpriteDe(int jugada)
    {
        switch (jugada)
        {
            case (int)Jugada.Piedra: return spritePiedra;
            case (int)Jugada.Papel: return spritePapel;
            default: return spriteTijera;
        }
    }

    private void MostrarEleccion(Image imagen, TMP_Text nombre, int jugada)
    {
        if (imagen != null)
        {
            imagen.enabled = true;
            imagen.sprite = SpriteDe(jugada);
            imagen.color = Color.white;
        }

        if (nombre != null)
            nombre.text = Nombres[jugada];
    }

    private void PonerOculto(Image imagen, TMP_Text nombre, string textoNombre)
    {
        if (imagen != null)
        {
            Aplicar(imagen.rectTransform, 1f);
            imagen.rectTransform.localRotation = Quaternion.identity;
            imagen.color = Color.white;

            if (spriteOculto != null)
            {
                imagen.enabled = true;
                imagen.sprite = spriteOculto;
            }
            else
            {
                imagen.enabled = false;
            }
        }

        if (nombre != null)
            nombre.text = textoNombre;
    }

    private void ReiniciarImagenes()
    {
        PonerOculto(imagenJugador, nombreJugador, string.Empty);
        PonerOculto(imagenVendedor, nombreVendedor, string.Empty);
    }

    private void MostrarPuntaje(int jugador, int vendedor)
    {
        textoPuntaje.text = $"Vos {jugador} - {vendedor} Horacio";
    }

    private void PonerBotones(bool activos)
    {
        botonPiedra.interactable = activos;
        botonPapel.interactable = activos;
        botonTijera.interactable = activos;
    }

    // Aplica una escala uniforme (espejando la imagen de Horacio si corresponde).
    private void Aplicar(RectTransform rt, float escala)
    {
        float signo = (espejarVendedor && imagenVendedor != null && rt == imagenVendedor.rectTransform) ? -1f : 1f;
        rt.localScale = new Vector3(escala * signo, escala, 1f);
    }

    // ───────────────────────── ANIMACIONES ─────────────────────────

    // El puño "tiembla" mientras Horacio piensa.
    private IEnumerator Temblar(RectTransform rt)
    {
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 12f);
            Aplicar(rt, 1f + Mathf.Abs(Mathf.Sin(t * 15f)) * 0.08f);
            yield return null;
        }
    }

    // La imagen aparece creciendo con un pequeño rebote.
    private IEnumerator Pop(RectTransform rt)
    {
        rt.localRotation = Quaternion.identity;

        const float subida = 0.2f;
        const float rebote = 0.1f;

        for (float t = 0f; t < subida; t += Time.deltaTime)
        {
            Aplicar(rt, Mathf.Lerp(0.3f, 1.15f, t / subida));
            yield return null;
        }

        for (float t = 0f; t < rebote; t += Time.deltaTime)
        {
            Aplicar(rt, Mathf.Lerp(1.15f, 1f, t / rebote));
            yield return null;
        }

        Aplicar(rt, 1f);
    }

    private void DetenerTemblor()
    {
        if (rutinaTemblor != null)
        {
            StopCoroutine(rutinaTemblor);
            rutinaTemblor = null;
        }

        if (imagenVendedor != null)
        {
            imagenVendedor.rectTransform.localRotation = Quaternion.identity;
            Aplicar(imagenVendedor.rectTransform, 1f);
        }
    }

    private void DetenerAnimaciones()
    {
        DetenerTemblor();

        if (rutinaRonda != null)
        {
            StopCoroutine(rutinaRonda);
            rutinaRonda = null;
        }
    }
}