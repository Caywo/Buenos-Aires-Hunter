using UnityEngine;

// Va en el objeto "Golpe" (Image + CanvasGroup con Alpha 0).
[RequireComponent(typeof(CanvasGroup))]
public class HitEffect : MonoBehaviour
{
    [SerializeField] float intensidadMaxima = 0.8f;
    [SerializeField] float duracion = 0.5f;

    CanvasGroup grupo;
    float tiempo;
    float intensidadActual;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
    }

    // intensidad: 0 a 1 (1 = golpe fuerte)
    public void Mostrar(float intensidad = 1f)
    {
        intensidadActual = intensidadMaxima * Mathf.Clamp01(intensidad);
        tiempo = duracion;
        grupo.alpha = intensidadActual;
    }

    void Update()
    {
        if (tiempo <= 0f) return;

        tiempo -= Time.unscaledDeltaTime;
        grupo.alpha = Mathf.Lerp(0f, intensidadActual, Mathf.Max(0f, tiempo) / duracion);
    }
}
