using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Botón que abre un panel para elegir entre Normal y Difícil.</summary>
public class SelectorDificultadUI : MonoBehaviour
{
    [Header("Botón principal")]
    [SerializeField] private Button botonAbrir;
    [Tooltip("Texto del botón principal (muestra la dificultad actual).")]
    [SerializeField] private TMP_Text textoBotonAbrir;

    [Header("Panel de selección")]
    [SerializeField] private GameObject panelSeleccion;
    [SerializeField] private Button botonNormal;
    [SerializeField] private Button botonDificil;

    [Header("Marcas de selección (opcional)")]
    [Tooltip("Objeto (por ejemplo un tilde) que se muestra junto a la dificultad elegida.")]
    [SerializeField] private GameObject marcaNormal;
    [SerializeField] private GameObject marcaDificil;

    private void Awake()
    {
        botonAbrir.onClick.AddListener(AlternarPanel);
        botonNormal.onClick.AddListener(() => Elegir(NivelDificultad.Normal));
        botonDificil.onClick.AddListener(() => Elegir(NivelDificultad.Dificil));

        panelSeleccion.SetActive(false);
        Refrescar();
    }

    private void OnEnable()
    {
        Refrescar();
    }

    private void AlternarPanel()
    {
        panelSeleccion.SetActive(!panelSeleccion.activeSelf);
    }

    private void Elegir(NivelDificultad nuevo)
    {
        Dificultad.Actual = nuevo;
        Refrescar();
        panelSeleccion.SetActive(false);
    }

    private void Refrescar()
    {
        bool dificil = Dificultad.EsDificil;

        if (textoBotonAbrir != null)
            textoBotonAbrir.text = dificil ? "DIFICULTAD: DIFÍCIL" : "DIFICULTAD: NORMAL";

        if (marcaNormal != null) marcaNormal.SetActive(!dificil);
        if (marcaDificil != null) marcaDificil.SetActive(dificil);
    }
}