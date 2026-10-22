using UnityEngine;
using UnityEngine.UI;

// Va en cada Slot del hotbar. Solo muestra el estado, no decide nada.
public class SlotUI : MonoBehaviour
{
    public enum Estado { Bloqueado, Desbloqueado, Seleccionado }

    [Header("Marcos (se activa solo uno)")]
    [SerializeField] GameObject frameBloqueado;
    [SerializeField] GameObject frameDesbloqueado;
    [SerializeField] GameObject frameSeleccionado;

    [Header("Icono")]
    [SerializeField] Image icono;
    [SerializeField] Color colorBloqueado = new Color(0.25f, 0.25f, 0.25f, 0.5f);

    public void SetEstado(Estado estado)
    {
        if (frameBloqueado != null) frameBloqueado.SetActive(estado == Estado.Bloqueado);
        if (frameDesbloqueado != null) frameDesbloqueado.SetActive(estado == Estado.Desbloqueado);
        if (frameSeleccionado != null) frameSeleccionado.SetActive(estado == Estado.Seleccionado);

        if (icono != null)
            icono.color = estado == Estado.Bloqueado ? colorBloqueado : Color.white;
    }
}
