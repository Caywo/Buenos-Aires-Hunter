using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Agranda el botón al pasar el mouse por encima (solo si está habilitado).</summary>
[RequireComponent(typeof(Button))]
public class EfectoBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float escalaHover = 1.12f;
    [SerializeField] private float velocidad = 12f;

    private Button boton;
    private Vector3 escalaBase;
    private bool encima;

    private void Awake()
    {
        boton = GetComponent<Button>();
        escalaBase = transform.localScale;
    }

    private void OnEnable()
    {
        encima = false;
        transform.localScale = escalaBase;
    }

    public void OnPointerEnter(PointerEventData e) { encima = true; }
    public void OnPointerExit(PointerEventData e) { encima = false; }

    private void Update()
    {
        float objetivo = (encima && boton.interactable) ? escalaHover : 1f;

        transform.localScale = Vector3.Lerp(
            transform.localScale, escalaBase * objetivo, velocidad * Time.unscaledDeltaTime);
    }
}