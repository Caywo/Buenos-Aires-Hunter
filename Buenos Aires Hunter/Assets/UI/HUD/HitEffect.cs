using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HitEffect : MonoBehaviour
{
    [SerializeField] float intensidadMaxima = 0.8f;
    [SerializeField] float duracion = 0.5f;

    CanvasGroup grupo;
    float tiempo;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        tiempo = 0f;
    }

    public void Mostrar()
    {
        tiempo = duracion;
        grupo.alpha = intensidadMaxima;
    }

    void Update()
    {
        if (tiempo <= 0f) return;

        tiempo -= Time.unscaledDeltaTime;
        grupo.alpha = Mathf.Lerp(0f, intensidadMaxima, tiempo / duracion);
    }
}