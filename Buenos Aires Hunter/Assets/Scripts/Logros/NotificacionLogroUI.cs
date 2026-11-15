using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NotificacionLogroUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private LogroUI tarjetaLogro; // Instancia de tu prefab LogroUI colocada en el HUD
    [SerializeField] private CanvasGroup canvasGroup; // Opcional, para fade in/out suave
    [SerializeField] private List<LogroData> todosLosLogros;

    [Header("Tiempos")]
    [SerializeField] private float duracionVisible = 4f;
    [SerializeField] private float tiempoFade = 0.4f;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoLogro;

    private readonly Dictionary<string, LogroData> catalogo = new Dictionary<string, LogroData>();
    private readonly Queue<LogroData> colaNotificaciones = new Queue<LogroData>();
    private bool mostrando;

    private void Awake()
    {
        foreach (var data in todosLosLogros)
        {
            if (data != null && !catalogo.ContainsKey(data.id))
            {
                catalogo[data.id] = data;
            }
        }

        if (tarjetaLogro != null)
            tarjetaLogro.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (LogrosManager.Instance != null)
        {
            LogrosManager.Instance.AlDesbloquear += EncolarLogro;
        }
    }

    private void Start()
    {
        // Respaldo por si LogrosManager se inicializó después de OnEnable
        if (LogrosManager.Instance != null)
        {
            LogrosManager.Instance.AlDesbloquear -= EncolarLogro;
            LogrosManager.Instance.AlDesbloquear += EncolarLogro;
        }
    }

    private void OnDisable()
    {
        if (LogrosManager.Instance != null)
        {
            LogrosManager.Instance.AlDesbloquear -= EncolarLogro;
        }
    }

    private void EncolarLogro(string id)
    {
        // Asegurarnos de que solo lo muestre el HUD del jugador local
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        if (catalogo.TryGetValue(id, out LogroData data))
        {
            colaNotificaciones.Enqueue(data);
            if (!mostrando)
            {
                StartCoroutine(ProcesarCola());
            }
        }
    }

    private IEnumerator ProcesarCola()
    {
        mostrando = true;

        while (colaNotificaciones.Count > 0)
        {
            LogroData actual = colaNotificaciones.Dequeue();

            if (tarjetaLogro != null)
            {
                tarjetaLogro.gameObject.SetActive(true);
                tarjetaLogro.Configurar(actual);
                tarjetaLogro.SetDesbloqueado(true); // Fuerza el estado desbloqueado (revela secretos)
            }

            if (audioSource != null && sonidoLogro != null)
            {
                audioSource.PlayOneShot(sonidoLogro);
            }

            // Fade In (usa unscaledDeltaTime para que funcione incluso si el juego está en pausa)
            yield return MoverAlpha(0f, 1f, tiempoFade);

            yield return new WaitForSecondsRealtime(duracionVisible);

            // Fade Out
            yield return MoverAlpha(1f, 0f, tiempoFade);

            if (tarjetaLogro != null)
                tarjetaLogro.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(0.25f);
        }

        mostrando = false;
    }

    private IEnumerator MoverAlpha(float desde, float hasta, float duracion)
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        canvasGroup.alpha = desde;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }

        canvasGroup.alpha = hasta;
    }
}