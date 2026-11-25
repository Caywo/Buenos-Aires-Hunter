using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Cartel de aviso en pantalla. Muestra un mensaje un momento y se oculta solo.
/// Si llega otro mensaje mientras está visible, lo reemplaza.
/// </summary>
public class CartelAvisoUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto ImgCartel (el recuadro completo).")]
    [SerializeField] private RectTransform contenedor;
    [Tooltip("Texto del título (AVISO).")]
    [SerializeField] private TMP_Text textoTitulo;
    [Tooltip("Texto del mensaje (TextoAviso).")]
    [SerializeField] private TMP_Text textoMensaje;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float duracionPorDefecto = 2.5f;
    [SerializeField] private float tiempoAparecer = 0.15f;
    [SerializeField] private float tiempoDesaparecer = 0.25f;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioSource fuenteSonido;
    [SerializeField] private AudioClip sonidoAviso;

    private CanvasGroup grupo;
    private Coroutine rutina;
    private string tituloPorDefecto = "AVISO";
    private string mensajeActual;

    private void Awake()
    {
        grupo = contenedor.GetComponent<CanvasGroup>();

        if (grupo == null)
            grupo = contenedor.gameObject.AddComponent<CanvasGroup>();

        // El cartel nunca debe tapar clics.
        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        grupo.alpha = 0f;

        // El título que dejaste escrito en el prefab es el título por defecto.
        if (textoTitulo != null && !string.IsNullOrWhiteSpace(textoTitulo.text))
            tituloPorDefecto = textoTitulo.text;

        contenedor.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        rutina = null;

        if (contenedor != null)
            contenedor.gameObject.SetActive(false);
    }

    /// <summary>Muestra un aviso. Titulo y duración son opcionales.</summary>
    public void Mostrar(string mensaje, string titulo = null, float duracion = -1f)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        if (duracion <= 0f)
            duracion = duracionPorDefecto;

        // Si es el mismo mensaje que ya se está mostrando, solo se reinicia el tiempo (sin repetir la entrada ni el sonido).
        bool repetido = rutina != null && mensaje == mensajeActual;
        mensajeActual = mensaje;

        if (textoTitulo != null)
            textoTitulo.text = string.IsNullOrEmpty(titulo) ? tituloPorDefecto : titulo;

        if (textoMensaje != null)
            textoMensaje.text = mensaje;

        if (rutina != null)
            StopCoroutine(rutina);

        if (!repetido && fuenteSonido != null && sonidoAviso != null)
            fuenteSonido.PlayOneShot(sonidoAviso);

        rutina = StartCoroutine(Animar(duracion, !repetido));
    }

    /// <summary>Oculta el cartel ya mismo.</summary>
    public void Ocultar()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        grupo.alpha = 0f;
        contenedor.gameObject.SetActive(false);
    }

    private IEnumerator Animar(float duracion, bool conEntrada)
    {
        contenedor.gameObject.SetActive(true);

        // Tiempo sin escala: el cartel funciona también con el juego en pausa.
        if (conEntrada)
        {
            for (float t = 0f; t < tiempoAparecer; t += Time.unscaledDeltaTime)
            {
                float k = t / tiempoAparecer;
                grupo.alpha = k;
                contenedor.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
                yield return null;
            }
        }

        grupo.alpha = 1f;
        contenedor.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(duracion);

        for (float t = 0f; t < tiempoDesaparecer; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = 1f - t / tiempoDesaparecer;
            yield return null;
        }

        grupo.alpha = 0f;
        contenedor.gameObject.SetActive(false);
        rutina = null;
    }
}