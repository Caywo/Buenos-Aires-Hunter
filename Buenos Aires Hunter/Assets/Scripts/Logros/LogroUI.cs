using UnityEngine;
using TMPro;

public class LogroUI : MonoBehaviour
{
    [SerializeField] TMP_Text txtTitulo;
    [SerializeField] TMP_Text txtDescripcion;
    [SerializeField] GameObject frameBloqueado;     // opcional
    [SerializeField] GameObject frameDesbloqueado;
    [SerializeField] GameObject velo;
    [SerializeField] string textoSecreto = "???";

    public LogroData Data { get; private set; }
    public bool Desbloqueado { get; private set; }

    public void Configurar(LogroData data)
    {
        Data = data;
        SetDesbloqueado(false);
    }

    public void SetDesbloqueado(bool desbloqueado)
    {
        Desbloqueado = desbloqueado;

        bool ocultar = Data.secreto && !desbloqueado;
        txtTitulo.text = ocultar ? textoSecreto : Data.titulo;
        txtDescripcion.text = ocultar ? "" : Data.descripcion;

        if (frameBloqueado != null) frameBloqueado.SetActive(!desbloqueado);
        if (frameDesbloqueado != null) frameDesbloqueado.SetActive(desbloqueado);
        if (velo != null) velo.SetActive(!desbloqueado);
    }
}