using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorUIOpciones : MonoBehaviour
{
    [Header("Referencias UI - Sliders")]
    public TMP_Dropdown dropdownResolucion;
    public Slider sliderSensibilidadX;
    public Slider sliderSensibilidadY;
    public Slider sliderMusica;
    public Slider sliderSonidos;

    [Header("Referencias UI - Textos Numéricos")]
    public TMP_Text txtValorSensX;
    public TMP_Text txtValorSensY;
    public TMP_Text txtValorMusica;
    public TMP_Text txtValorSonidos;

    [Header("Botones")]
    public Button btnRestablecer;

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

    public void SincronizarUI()
    {
        if (Configuracion.Instance == null) return;

        var config = Configuracion.Instance;

        // --- SENSIBILIDAD X ---
        if (sliderSensibilidadX != null)
        {
            sliderSensibilidadX.onValueChanged.RemoveAllListeners();
            sliderSensibilidadX.value = config.SensibilidadX;
            ActualizarTextoSensX(config.SensibilidadX);

            sliderSensibilidadX.onValueChanged.AddListener(v => {
                config.SetSensibilidadX(v);
                ActualizarTextoSensX(v);
            });
        }

        // --- SENSIBILIDAD Y ---
        if (sliderSensibilidadY != null)
        {
            sliderSensibilidadY.onValueChanged.RemoveAllListeners();
            sliderSensibilidadY.value = config.SensibilidadY;
            ActualizarTextoSensY(config.SensibilidadY);

            sliderSensibilidadY.onValueChanged.AddListener(v => {
                config.SetSensibilidadY(v);
                ActualizarTextoSensY(v);
            });
        }

        // --- MÚSICA (%) ---
        if (sliderMusica != null)
        {
            sliderMusica.onValueChanged.RemoveAllListeners();
            sliderMusica.value = config.VolumenMusica;
            ActualizarTextoMusica(config.VolumenMusica);

            sliderMusica.onValueChanged.AddListener(v => {
                config.SetVolumenMusica(v);
                ActualizarTextoMusica(v);
            });
        }

        // --- SONIDOS (%) ---
        if (sliderSonidos != null)
        {
            sliderSonidos.onValueChanged.RemoveAllListeners();
            sliderSonidos.value = config.VolumenSonidos;
            ActualizarTextoSonidos(config.VolumenSonidos);

            sliderSonidos.onValueChanged.AddListener(v => {
                config.SetVolumenSonidos(v);
                ActualizarTextoSonidos(v);
            });
        }

        // --- RESOLUCIÓN ---
        if (dropdownResolucion != null)
        {
            dropdownResolucion.onValueChanged.RemoveAllListeners();
            dropdownResolucion.onValueChanged.AddListener(config.SetResolution);
        }
    }

    // ───────────────────────── Formateo de Textos ─────────────────────────

    private void ActualizarTextoSensX(float valor)
    {
        if (txtValorSensX != null) 
            txtValorSensX.text = valor.ToString("F1"); // Muestra 1 decimal (ej: 5.5)
    }

    private void ActualizarTextoSensY(float valor)
    {
        if (txtValorSensY != null) 
            txtValorSensY.text = valor.ToString("F1"); // Muestra 1 decimal (ej: 5.5)
    }

    private void ActualizarTextoMusica(float valor)
    {
        if (txtValorMusica != null) 
            txtValorMusica.text = Mathf.RoundToInt(valor * 100f) + "%"; // Muestra porcentaje (ej: 75%)
    }

    private void ActualizarTextoSonidos(float valor)
    {
        if (txtValorSonidos != null) 
            txtValorSonidos.text = Mathf.RoundToInt(valor * 100f) + "%"; // Muestra porcentaje (ej: 100%)
    }

    public void OnClicRestablecer()
    {
        if (Configuracion.Instance != null)
        {
            Configuracion.Instance.RestablecerPorDefecto();
            SincronizarUI();

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
        if (listaResoluciones == null || listaResoluciones.Count == 0) return;

        List<string> opciones = new List<string>();
        for (int i = 0; i < listaResoluciones.Count; i++)
        {
            string opcion = $"{listaResoluciones[i].width}x{listaResoluciones[i].height} {listaResoluciones[i].refreshRateRatio.value:0} Hz";
            opciones.Add(opcion);
        }

        dropdownResolucion.AddOptions(opciones);

        int savedIndex = Configuracion.Instance.ResolucionIndex;
        int indexMedio = listaResoluciones.Count / 2;
        int indexAUsar = (savedIndex >= 0 && savedIndex < listaResoluciones.Count) ? savedIndex : indexMedio;

        dropdownResolucion.value = indexAUsar;
        dropdownResolucion.RefreshShownValue();
    }
}