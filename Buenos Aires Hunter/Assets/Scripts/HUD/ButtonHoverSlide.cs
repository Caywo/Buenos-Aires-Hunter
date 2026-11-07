using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class ButtonHoverSlide : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Movimiento")]
    [SerializeField] RectTransform visual;
    [SerializeField] float desplazamiento = 30f;
    [SerializeField] float velocidad = 12f;

    [Header("Sonido")]
    [SerializeField] AudioClip sonidoHover;
    [SerializeField, Range(0f, 1f)] float volumen = 0.7f;

    AudioSource audioSource;
    Vector2 posOriginal;
    Vector2 posObjetivo;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        posOriginal = visual.anchoredPosition;
        posObjetivo = posOriginal;
    }

    void OnEnable()
    {
        visual.anchoredPosition = posOriginal;
        posObjetivo = posOriginal;
    }

    void Update()
    {
        visual.anchoredPosition = Vector2.Lerp(
            visual.anchoredPosition, posObjetivo, Time.unscaledDeltaTime * velocidad);
    }

    void Mover()
    {
        posObjetivo = posOriginal + Vector2.right * desplazamiento;
        if (sonidoHover != null)
            audioSource.PlayOneShot(sonidoHover, volumen);
    }

    void Volver() => posObjetivo = posOriginal;

    public void OnPointerEnter(PointerEventData e) => Mover();
    public void OnPointerExit(PointerEventData e) => Volver();
    public void OnSelect(BaseEventData e) => Mover();
    public void OnDeselect(BaseEventData e) => Volver();
}