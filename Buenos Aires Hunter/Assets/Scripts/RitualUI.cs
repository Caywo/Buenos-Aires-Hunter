using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Pantalla del ritual: contador de sacrificios y mensaje a pantalla completa.
/// </summary>
public class RitualUI : MonoBehaviour
{
    [Header("Contador")]
    [SerializeField] private GameObject contador;
    [SerializeField] private TMP_Text textoContador;
    [SerializeField] private string formatoContador = "Sacrificios: {0}/{1}";

    [Header("Mensaje a pantalla completa")]
    [SerializeField] private CanvasGroup grupoMensaje;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoSubtitulo;
    [SerializeField] private float fundidoEntrada = 0.6f;
    [SerializeField] private float fundidoSalida = 1f;

    private Coroutine rutina;
    private bool contadorVisible;

    private void Awake()
    {
        if (grupoMensaje != null)
        {
            grupoMensaje.alpha = 0f;
            grupoMensaje.blocksRaycasts = false;
            grupoMensaje.interactable = false;
        }

        if (contador != null)
            contador.SetActive(false);
    }

    private void Update()
    {
        RitualBoss ritual = RitualBoss.Instancia;
        bool mostrar = ritual != null && ritual.Estado == EstadoRitual.EnCurso;

        if (mostrar != contadorVisible)
        {
            contadorVisible = mostrar;

            if (contador != null)
                contador.SetActive(mostrar);
        }

        if (mostrar && textoContador != null)
            textoContador.text = string.Format(formatoContador, ritual.Sacrificios, ritual.Necesarios);
    }

    /// <summary>Muestra un mensaje grande a pantalla completa durante 'duracion' segundos.</summary>
    public void MostrarMensaje(string titulo, string subtitulo, float duracion)
    {
        if (grupoMensaje == null)
            return;

        if (rutina != null)
            StopCoroutine(rutina);

        textoTitulo.text = titulo;

        if (textoSubtitulo != null)
            textoSubtitulo.text = subtitulo;

        rutina = StartCoroutine(AnimarMensaje(duracion));
    }

    private IEnumerator AnimarMensaje(float duracion)
    {
        for (float t = 0f; t < fundidoEntrada; t += Time.deltaTime)
        {
            grupoMensaje.alpha = t / fundidoEntrada;
            yield return null;
        }

        grupoMensaje.alpha = 1f;

        float espera = Mathf.Max(0f, duracion - fundidoEntrada - fundidoSalida);
        yield return new WaitForSeconds(espera);

        for (float t = 0f; t < fundidoSalida; t += Time.deltaTime)
        {
            grupoMensaje.alpha = 1f - t / fundidoSalida;
            yield return null;
        }

        grupoMensaje.alpha = 0f;
        rutina = null;
    }
}