using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InfoPanelHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Panel compartido")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text txtTitulo;
    [SerializeField] TMP_Text txtDescripcion;

    [Header("Contenido de este modo")]
    [SerializeField] string titulo = "EXTRACCIÓN";
    [TextArea(3, 8)]
    [SerializeField] string descripcion = "Descripción del modo de juego.";

    void Mostrar()
    {
        if (txtTitulo != null) txtTitulo.text = titulo;
        if (txtDescripcion != null) txtDescripcion.text = descripcion;
        if (panel != null) panel.SetActive(true);
    }

    void Ocultar()
    {
        if (panel != null) panel.SetActive(false);
    }

    void OnDisable() => Ocultar();   // si se cambia de pantalla, el panel no queda abierto

    public void OnPointerEnter(PointerEventData e) => Mostrar();
    public void OnPointerExit(PointerEventData e) => Ocultar();
    public void OnSelect(BaseEventData e) => Mostrar();      // teclado / joystick
    public void OnDeselect(BaseEventData e) => Ocultar();
}