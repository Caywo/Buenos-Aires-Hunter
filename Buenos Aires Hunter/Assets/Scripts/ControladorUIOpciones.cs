using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorUIOpciones : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Dropdown dropdownResolucion;
    public Slider sliderSensibilidadX;
    public Slider sliderSensibilidadY;
    public Slider sliderMusica;
    public Slider sliderSonidos;

    [Header("Botones")]
    public Button btnRestablecer; // Referencia al botón UI

    private void OnEnable()
    {
        SincronizarUI();
    }

    private void Start()
    {
        InicializarDropdownResolucion();
        SincronizarUI();

        if (btnRestablecer != null)
        {
            btnRestablecer.onClick.RemoveAllListeners();
            btnRestablecer.onClick.AddListener(OnClicRestablecer);
        }
    }

    public void OnClicRestablecer()
    {
        if (Configuracion.Instance != null)
        {
            // 1. Restablece los datos de backend en el Singleton
            Configuracion.Instance.RestablecerPorDefecto();
            
            // 2. Refresca la UI con los nuevos valores
            SincronizarUI();
            
            // 3. Ajusta el Dropdown de resolución a la opción media
            if (dropdownResolucion != null)
            {
                dropdownResolucion.value = Configuracion.Instance.ResolucionIndex;
                dropdownResolucion.RefreshShownValue();
            }
        }
    }


    private void InicializarDropdownResolucion()
    {
        if (dropdownResolucion == null || Configuracion.Instance == null) return;

        dropdownResolucion.ClearOptions();
        var listaResoluciones = Configuracion.Instance.ResolucionesFiltradas;
        if (listaResoluciones == null) return;

        List<string> opciones = new List<string>();
        int indiceActual = 0;

        for (int i = 0; i < listaResoluciones.Count; i++)
        {
            string opcion = $"{listaResoluciones[i].width}x{listaResoluciones[i].height} {listaResoluciones[i].refreshRateRatio.value:0} Hz";
            opciones.Add(opcion);

            if (listaResoluciones[i].width == Screen.width && listaResoluciones[i].height == Screen.height)
            {
                indiceActual = i;
            }
        }

        dropdownResolucion.AddOptions(opciones);

        int savedIndex = Configuracion.Instance.ResolucionIndex;
        int indexAUsar = (savedIndex >= 0 && savedIndex < listaResoluciones.Count) ? savedIndex : indiceActual;

        dropdownResolucion.value = indexAUsar;
        dropdownResolucion.RefreshShownValue();
    }

    public void SincronizarUI()
    {
        if (Configuracion.Instance == null) return;

        var config = Configuracion.Instance;

        if (sliderSensibilidadX != null)
        {
            sliderSensibilidadX.onValueChanged.RemoveAllListeners();
            sliderSensibilidadX.value = config.SensibilidadX;
            sliderSensibilidadX.onValueChanged.AddListener(config.SetSensibilidadX);
        }

        if (sliderSensibilidadY != null)
        {
            sliderSensibilidadY.onValueChanged.RemoveAllListeners();
            sliderSensibilidadY.value = config.SensibilidadY;
            sliderSensibilidadY.onValueChanged.AddListener(config.SetSensibilidadY);
        }

        if (sliderMusica != null)
        {
            sliderMusica.onValueChanged.RemoveAllListeners();
            sliderMusica.value = config.VolumenMusica;
            sliderMusica.onValueChanged.AddListener(config.SetVolumenMusica);
        }

        if (sliderSonidos != null)
        {
            sliderSonidos.onValueChanged.RemoveAllListeners();
            sliderSonidos.value = config.VolumenSonidos;
            sliderSonidos.onValueChanged.AddListener(config.SetVolumenSonidos);
        }

        if (dropdownResolucion != null)
        {
            dropdownResolucion.onValueChanged.RemoveAllListeners();
            dropdownResolucion.onValueChanged.AddListener(config.SetResolution);
        }
    }
}