using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinijuegoPPTUI : MonoBehaviour
{
    [Header("Pantalla")]
    [SerializeField] private GameObject panel;

    [Header("Botones de jugada")]
    [SerializeField] private Button botonPiedra;
    [SerializeField] private Button botonPapel;
    [SerializeField] private Button botonTijera;
    [Tooltip("Cierra el juego y vuelve a la tienda.")]
    [SerializeField] private Button botonCerrar;

    [Header("Apuesta")]
    [Tooltip("Grupo con los controles de apuesta (se muestra antes de cada partida).")]
    [SerializeField] private GameObject grupoApuesta;
    [SerializeField] private TMP_Text textoApuesta;
    [SerializeField] private Button botonMenos;
    [SerializeField] private Button botonMas;
    [SerializeField] private Button botonApostar;
    [Tooltip("Muestra el saldo y las partidas que quedan.")]
    [SerializeField] private TMP_Text textoInfo;

    [Header("Elecciones (imágenes)")]
    [SerializeField] private Image imagenJugador;
    [SerializeField] private Image imagenVendedor;
    [SerializeField] private Sprite spritePiedra;
    [SerializeField] private Sprite spritePapel;
    [SerializeField] private Sprite spriteTijera;
    [Tooltip("Imagen antes de revelar (el medallón con '?').")]
    [SerializeField] private Sprite spriteOculto;
    [SerializeField] private bool espejarVendedor = false;

    [Header("Brillo del resultado (opcional)")]
    [SerializeField] private Image brilloJugador;
    [SerializeField] private Image brilloVendedor;

    [Header("Puntos de victoria (opcional)")]
    [SerializeField] private Image[] puntosJugador;
    [SerializeField] private Image[] puntosVendedor;
    [SerializeField] private Sprite spritePuntoLleno;
    [SerializeField] private Sprite spritePuntoVacio;

    [Header("Textos")]
    [Tooltip("Opcional: si usás los puntos de victoria, podés dejarlo vacío.")]
    [SerializeField] private TMP_Text textoPuntaje;
    [SerializeField] private TMP_Text textoMensaje;
    [Tooltip("Nombre de la jugada debajo de cada imagen. Opcional.")]
    [SerializeField] private TMP_Text nombreJugador;
    [SerializeField] private TMP_Text nombreVendedor;

    [Header("Animación")]
    [SerializeField] private float duracionSuspenso = 1.2f;
    [SerializeField] private float pausaEntreRondas = 1.5f;
    [SerializeField] private Color colorGana = new Color(0.35f, 1f, 0.35f, 0.9f);
    [SerializeField] private Color colorPierde = new Color(1f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private Color colorEmpate = new Color(1f, 1f, 1f, 0.6f);

    private static readonly string[] Nombres = { "Piedra", "Papel", "Tijera" };
    private static readonly string[] Cuenta = { "Piedra...", "Papel...", "¡Tijera!" };

    private MinijuegoPiedraPapelTijera juego;
    private PlayerInventory jugadorActual;
    private TiendaUI tiendaOrigen;

    private int apuesta;
    private bool enPartida;
    private bool esperando;
    private bool terminada;
    private float inicioEspera;
    private float proximoRefresco;

    private Coroutine rutinaTemblor;
    private Coroutine rutinaCuenta;
    private Coroutine rutinaRonda;

    public bool EstaAbierta => panel.activeSelf;

    private void Awake()
    {
        botonPiedra.onClick.AddListener(() => Elegir(Jugada.Piedra));
        botonPapel.onClick.AddListener(() => Elegir(Jugada.Papel));
        botonTijera.onClick.AddListener(() => Elegir(Jugada.Tijera));
        botonCerrar.onClick.AddListener(Cerrar);

        if (botonMenos != null) botonMenos.onClick.AddListener(() => CambiarApuesta(-1));
        if (botonMas != null) botonMas.onClick.AddListener(() => CambiarApuesta(1));
        if (botonApostar != null) botonApostar.onClick.AddListener(Apostar);

        if (imagenJugador != null) imagenJugador.preserveAspect = true;
        if (imagenVendedor != null) imagenVendedor.preserveAspect = true;

        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        Desuscribir();
    }

    // Mientras se elige la apuesta, refresca el saldo y las partidas (cambian con las bajas y las oleadas).
    private void Update()
    {
        if (!panel.activeSelf || enPartida || grupoApuesta == null || !grupoApuesta.activeSelf)
            return;

        if (Time.unscaledTime < proximoRefresco)
            return;

        proximoRefresco = Time.unscaledTime + 0.25f;

        if (!esperando)
            ActualizarApuestaUI();
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
            juego.OnError += AlRecibirError;
        }

        panel.SetActive(true);
        BloquearControles(true);

        DetenerAnimaciones();
        ReiniciarImagenes();
        ActualizarPuntos(0, 0);

        if (textoPuntaje != null) textoPuntaje.text = string.Empty;
        PonerBotones(false);

        esperando = false;

        if (juego.HayPartidaEnCurso)
        {
            // Había una partida abierta: se retoma sin cobrar de nuevo.
            textoMensaje.text = "Retomando la partida...";
            SetBotonesJugada(true);
            if (grupoApuesta != null) grupoApuesta.SetActive(false);
            juego.Iniciar(0);
        }
        else if (grupoApuesta != null)
        {
            MostrarModoApuesta("Elegí cuánto apostar");
        }
        else
        {
            Debug.LogWarning("[Minijuego] Falta configurar el Grupo Apuesta: se apuesta el mínimo.");
            juego.Iniciar(juego.ApuestaMinima);
        }
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
        juego.OnError -= AlRecibirError;
        juego = null;
    }

    // ───────────────────────── APUESTA ─────────────────────────

    private void MostrarModoApuesta(string mensaje)
    {
        enPartida = false;
        terminada = false;
        esperando = false;

        SetBotonesJugada(false);

        if (grupoApuesta != null)
            grupoApuesta.SetActive(true);

        if (apuesta <= 0)
            apuesta = juego.ApuestaMinima;

        ActualizarApuestaUI();

        if (!string.IsNullOrEmpty(mensaje))
            textoMensaje.text = mensaje;
    }

    private void MostrarModoJuego()
    {
        if (grupoApuesta != null)
            grupoApuesta.SetActive(false);

        SetBotonesJugada(true);
    }

    private void CambiarApuesta(int direccion)
    {
        if (juego == null || esperando)
            return;

        apuesta += direccion * juego.PasoApuesta;
        ActualizarApuestaUI();
    }

    private void ActualizarApuestaUI()
    {
        if (juego == null || jugadorActual == null)
            return;

        int saldo = jugadorActual.saldoSube.Value;
        int paso = juego.PasoApuesta;
        int minimo = juego.ApuestaMinima;

        // La apuesta máxima permitida es la del juego o lo que alcance el saldo (de a "paso").
        int maxPorSaldo = minimo + Mathf.Max(0, (saldo - minimo) / paso) * paso;
        int maxPermitida = Mathf.Min(juego.ApuestaMaxima, maxPorSaldo);

        apuesta = Mathf.Clamp(apuesta, minimo, Mathf.Max(minimo, maxPermitida));
        apuesta = minimo + ((apuesta - minimo) / paso) * paso;

        bool hayPartidas = juego.PartidasDisponibles > 0;
        bool alcanzaSaldo = saldo >= minimo;

        if (textoApuesta != null)
            textoApuesta.text = Plata(apuesta);

        if (botonMenos != null)
            botonMenos.interactable = apuesta > minimo;

        if (botonMas != null)
            botonMas.interactable = apuesta + paso <= maxPermitida;

        if (botonApostar != null)
            botonApostar.interactable = hayPartidas && alcanzaSaldo && apuesta <= saldo;

        if (textoInfo != null)
        {
            string aviso = !hayPartidas ? "  (sin partidas por ahora)"
                         : !alcanzaSaldo ? "  (saldo insuficiente)"
                         : string.Empty;

            textoInfo.text = $"Saldo: {Plata(saldo)}   |   Partidas: {juego.PartidasDisponibles}{aviso}";
        }
    }

    private void Apostar()
    {
        if (esperando || juego == null)
            return;

        esperando = true;

        if (botonApostar != null)
            botonApostar.interactable = false;

        textoMensaje.text = "Apostando...";
        juego.Iniciar(apuesta);
    }

    private static string Plata(int monto)
    {
        return monto < 0 ? $"-${-monto}" : $"${monto}";
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

        if (imagenVendedor != null)
            rutinaTemblor = StartCoroutine(Temblar(imagenVendedor.rectTransform));

        rutinaCuenta = StartCoroutine(CuentaRegresiva());

        juego.Jugar(jugada);
    }

    private void AlIniciar(int puntosJ, int puntosV, int apuestaServidor)
    {
        enPartida = true;
        esperando = false;
        terminada = false;

        DetenerAnimaciones();
        MostrarModoJuego();
        ReiniciarImagenes();
        ActualizarPuntos(puntosJ, puntosV);

        if (textoInfo != null)
            textoInfo.text = $"Apuesta: {Plata(apuestaServidor)}";

        textoMensaje.text = $"El primero en ganar {juego.VictoriasNecesarias} rondas gana. ¡Elegí!";
        PonerBotones(true);
    }

    private void AlRecibirError(string mensaje)
    {
        esperando = false;
        textoMensaje.text = mensaje;

        if (!enPartida)
            ActualizarApuestaUI();
    }

    private void AlRecibirRonda(int jugadaJugador, int jugadaVendedor, int resultado,
                                int puntosJ, int puntosV, bool termino, int cambioSaldo)
    {
        if (rutinaRonda != null)
            StopCoroutine(rutinaRonda);

        rutinaRonda = StartCoroutine(RevelarRonda(jugadaJugador, jugadaVendedor, resultado,
                                                  puntosJ, puntosV, termino, cambioSaldo));
    }

    private IEnumerator RevelarRonda(int jugadaJugador, int jugadaVendedor, int resultado,
                                     int puntosJ, int puntosV, bool termino, int cambioSaldo)
    {
        // Suspenso mínimo: Horacio "piensa" un momento aunque la respuesta llegue al instante.
        float restante = duracionSuspenso - (Time.time - inicioEspera);

        if (restante > 0f)
            yield return new WaitForSeconds(restante);

        DetenerTemblor();

        if (rutinaCuenta != null)
        {
            StopCoroutine(rutinaCuenta);
            rutinaCuenta = null;
        }

        // Revelación
        MostrarEleccion(imagenJugador, nombreJugador, jugadaJugador);
        MostrarEleccion(imagenVendedor, nombreVendedor, jugadaVendedor);

        PonerBrillo(brilloJugador, resultado == 1 ? colorGana : resultado == -1 ? colorPierde : colorEmpate);
        PonerBrillo(brilloVendedor, resultado == -1 ? colorGana : resultado == 1 ? colorPierde : colorEmpate);

        if (imagenVendedor != null)
            yield return StartCoroutine(Pop(imagenVendedor.rectTransform));

        ActualizarPuntos(puntosJ, puntosV);

        if (termino)
        {
            terminada = true;
            esperando = false;
            PonerBotones(false);

            string mensajeFinal = cambioSaldo > 0
                ? $"¡GANASTE! +${cambioSaldo}"
                : $"PERDISTE -${-cambioSaldo}";

            textoMensaje.text = mensajeFinal;

            yield return new WaitForSeconds(pausaEntreRondas + 0.5f);

            // Vuelve a la pantalla de apuesta (con el resultado a la vista).
            ReiniciarImagenes();
            ActualizarPuntos(0, 0);
            MostrarModoApuesta(mensajeFinal);

            rutinaRonda = null;
            yield break;
        }

        textoMensaje.text = resultado == 1 ? "¡Ganaste la ronda!"
                          : resultado == -1 ? "Perdiste la ronda"
                          : "Empate, se repite";

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

        if (brilloJugador != null) brilloJugador.enabled = false;
        if (brilloVendedor != null) brilloVendedor.enabled = false;
    }

    private void PonerBrillo(Image brillo, Color color)
    {
        if (brillo == null)
            return;

        brillo.enabled = true;
        brillo.color = color;
    }

    private void ActualizarPuntos(int puntosJ, int puntosV)
    {
        if (textoPuntaje != null)
            textoPuntaje.text = $"Vos {puntosJ} - {puntosV} Horacio";

        PintarPuntos(puntosJugador, puntosJ);
        PintarPuntos(puntosVendedor, puntosV);
    }

    private void PintarPuntos(Image[] fila, int puntos)
    {
        if (fila == null)
            return;

        int maximo = juego != null ? juego.VictoriasNecesarias : fila.Length;

        for (int i = 0; i < fila.Length; i++)
        {
            if (fila[i] == null)
                continue;

            fila[i].gameObject.SetActive(i < maximo);
            fila[i].sprite = i < puntos ? spritePuntoLleno : spritePuntoVacio;
        }
    }

    private void SetBotonesJugada(bool visibles)
    {
        botonPiedra.gameObject.SetActive(visibles);
        botonPapel.gameObject.SetActive(visibles);
        botonTijera.gameObject.SetActive(visibles);
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

    private IEnumerator CuentaRegresiva()
    {
        float paso = duracionSuspenso / Cuenta.Length;

        foreach (string palabra in Cuenta)
        {
            textoMensaje.text = palabra;
            yield return new WaitForSeconds(paso);
        }

        rutinaCuenta = null;
    }

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

        if (rutinaCuenta != null)
        {
            StopCoroutine(rutinaCuenta);
            rutinaCuenta = null;
        }

        if (rutinaRonda != null)
        {
            StopCoroutine(rutinaRonda);
            rutinaRonda = null;
        }
    }
}