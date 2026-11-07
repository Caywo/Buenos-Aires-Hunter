using System.Collections;
using TMPro;
using UnityEngine;

// Va en la raíz del prefab "PantallaCarga" (Canvas + CanvasGroup).
// Solo PRESENTA la pantalla: el dev decide cuándo llamar a Mostrar() y Ocultar().
[RequireComponent(typeof(CanvasGroup))]
public class PantallaCarga : MonoBehaviour
{
    public static PantallaCarga Instance { get; private set; }

    [Header("Texto")]
    [SerializeField] TMP_Text txtCargando;
    [SerializeField] string texto = "Cargando";
    [SerializeField] int maxPuntos = 3;
    [SerializeField] float intervaloPuntos = 0.4f;

    [Header("Comportamiento")]
    [SerializeField] float duracionFade = 0.3f;
    [Tooltip("Segundos mínimos en pantalla, para que no parpadee si la carga es muy rápida.")]
    [SerializeField] float tiempoMinimo = 1f;
    [Tooltip("Si está tildado, sobrevive a los cambios de escena (el objeto tiene que ser raíz de la jerarquía).")]
    [SerializeField] bool persistente = true;
    [SerializeField] bool visibleAlInicio = false;

    CanvasGroup grupo;
    Coroutine rutina;
    float inicioMostrada;
    float reloj;
    int puntos;

    public bool EstaVisible => grupo != null && grupo.alpha > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistente) DontDestroyOnLoad(gameObject);

        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = visibleAlInicio ? 1f : 0f;
        grupo.blocksRaycasts = visibleAlInicio;
        grupo.interactable = false;

        puntos = 0;
        ActualizarTexto();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Los puntos se escriben y se borran: "Cargando" → "Cargando." → ".." → "..." → vuelta a empezar
    void Update()
    {
        if (grupo.alpha <= 0f) return;

        reloj += Time.unscaledDeltaTime;
        if (reloj < intervaloPuntos) return;

        reloj -= intervaloPuntos;
        puntos = (puntos + 1) % (maxPuntos + 1);
        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        if (txtCargando == null) return;

        // Los puntos que faltan quedan transparentes (no invisibles) para que el texto no se mueva
        string visibles = new string('.', puntos);
        string ocultos = new string('.', maxPuntos - puntos);
        txtCargando.text = texto + visibles + "<alpha=#00>" + ocultos;
    }

    // ───────────────────────── API para el dev ─────────────────────────

    public void Mostrar()
    {
        if (rutina != null) StopCoroutine(rutina);

        inicioMostrada = Time.unscaledTime;
        puntos = 0;
        reloj = 0f;
        ActualizarTexto();

        grupo.alpha = 1f;               // aparece de golpe para tapar el cambio de escena
        grupo.blocksRaycasts = true;
    }

    public void Ocultar()
    {
        if (!gameObject.activeInHierarchy || !EstaVisible) return;

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(OcultarRutina());
    }

    IEnumerator OcultarRutina()
    {
        float falta = tiempoMinimo - (Time.unscaledTime - inicioMostrada);
        if (falta > 0f) yield return new WaitForSecondsRealtime(falta);

        float t = 0f;
        float alphaInicial = grupo.alpha;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(alphaInicial, 0f, t / duracionFade);
            yield return null;
        }

        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        rutina = null;
    }

    // Solo para probar el diseño (borrar antes de subir)
    [ContextMenu("Probar: mostrar y ocultar a los 3 segundos")]
    void Probar() => StartCoroutine(PruebaRutina());

    IEnumerator PruebaRutina()
    {
        Mostrar();
        yield return new WaitForSecondsRealtime(3f);
        Ocultar();
    }
}
