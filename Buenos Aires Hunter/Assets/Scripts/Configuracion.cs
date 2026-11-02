using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class Configuracion : MonoBehaviour
{
    public static Configuracion Instance { get; private set; }

    [Header("Referencias UI")]
    public TMP_Dropdown dropdownResolucion;
    public Slider sliderSensibilidadX; // Slider para X
    public Slider sliderSensibilidadY; // Slider para Y
    public Slider sliderMusica;
    public Slider sliderSonidos;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    // Keys para PlayerPrefs
    private const string KEY_SENS_X = "SensibilidadX";
    private const string KEY_SENS_Y = "SensibilidadY";
    private const string KEY_VOL_MUSICA = "VolumenMusica";
    private const string KEY_VOL_SONIDOS = "VolumenSonidos";
    private const string KEY_RES_INDEX = "ResolucionIndex";

    private Resolution[] resoluciones;
    private List<Resolution> resolucionesFiltradas;
    private RefreshRate hzActual;

    // Propiedades accesibles externamente
    public float SensibilidadX { get; private set; }
    public float SensibilidadY { get; private set; }
    public float VolumenMusica { get; private set; }
    public float VolumenSonidos { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CargarConfiguracion();
    }

    private void Start()
    {
        InicializarResoluciones();
        ConectarEventosUI();
    }

    private void CargarConfiguracion()
    {
        // Carga cada eje por separado (por defecto 5f)
        SensibilidadX = PlayerPrefs.GetFloat(KEY_SENS_X, 5f);
        SensibilidadY = PlayerPrefs.GetFloat(KEY_SENS_Y, 5f);
        VolumenMusica = PlayerPrefs.GetFloat(KEY_VOL_MUSICA, 0.75f);
        VolumenSonidos = PlayerPrefs.GetFloat(KEY_VOL_SONIDOS, 0.75f);

        AplicarAudio("VolumeMusica", VolumenMusica);
        AplicarAudio("VolumeSonidos", VolumenSonidos);
    }

    private void InicializarResoluciones()
    {
        if (dropdownResolucion == null) return;

        resoluciones = Screen.resolutions;
        resolucionesFiltradas = new List<Resolution>();
        dropdownResolucion.ClearOptions();

        hzActual = Screen.currentResolution.refreshRateRatio;

        for (int i = 0; i < resoluciones.Length; i++)
        {
            if (resoluciones[i].refreshRateRatio.value == hzActual.value)
            {
                resolucionesFiltradas.Add(resoluciones[i]);
            }
        }

        List<string> opciones = new List<string>();
        int indiceGuardado = PlayerPrefs.GetInt(KEY_RES_INDEX, -1);
        int indiceActual = 0;

        for (int i = 0; i < resolucionesFiltradas.Count; i++)
        {
            string opcionResolucion = $"{resolucionesFiltradas[i].width}x{resolucionesFiltradas[i].height} {resolucionesFiltradas[i].refreshRateRatio.value:0} Hz";
            opciones.Add(opcionResolucion);

            if (resolucionesFiltradas[i].width == Screen.width && resolucionesFiltradas[i].height == Screen.height)
            {
                indiceActual = i;
            }
        }

        dropdownResolucion.AddOptions(opciones);

        int indiceAUsar = (indiceGuardado >= 0 && indiceGuardado < resolucionesFiltradas.Count) ? indiceGuardado : indiceActual;
        dropdownResolucion.value = indiceAUsar;
        dropdownResolucion.RefreshShownValue();

        SetResolution(indiceAUsar);
    }

    private void ConectarEventosUI()
    {
        if (dropdownResolucion != null)
            dropdownResolucion.onValueChanged.AddListener(SetResolution);

        // Conectar Slider X
        if (sliderSensibilidadX != null)
        {
            sliderSensibilidadX.value = SensibilidadX;
            sliderSensibilidadX.onValueChanged.AddListener(SetSensibilidadX);
        }

        // Conectar Slider Y
        if (sliderSensibilidadY != null)
        {
            sliderSensibilidadY.value = SensibilidadY;
            sliderSensibilidadY.onValueChanged.AddListener(SetSensibilidadY);
        }

        if (sliderMusica != null)
        {
            sliderMusica.value = VolumenMusica;
            sliderMusica.onValueChanged.AddListener(SetVolumenMusica);
        }

        if (sliderSonidos != null)
        {
            sliderSonidos.value = VolumenSonidos;
            sliderSonidos.onValueChanged.AddListener(SetVolumenSonidos);
        }
    }

    public void SetResolution(int indiceResolucion)
    {
        if (resolucionesFiltradas == null || indiceResolucion >= resolucionesFiltradas.Count) return;

        Resolution resolucion = resolucionesFiltradas[indiceResolucion];
        Screen.SetResolution(resolucion.width, resolucion.height, FullScreenMode.FullScreenWindow, resolucion.refreshRateRatio);

        PlayerPrefs.SetInt(KEY_RES_INDEX, indiceResolucion);
        PlayerPrefs.Save();
    }

    // Métodos separados para cada eje
    public void SetSensibilidadX(float valor)
    {
        SensibilidadX = valor;
        PlayerPrefs.SetFloat(KEY_SENS_X, valor);
        PlayerPrefs.Save();
    }

    public void SetSensibilidadY(float valor)
    {
        SensibilidadY = valor;
        PlayerPrefs.SetFloat(KEY_SENS_Y, valor);
        PlayerPrefs.Save();
    }

    public void SetVolumenMusica(float valor)
    {
        VolumenMusica = valor;
        AplicarAudio("VolumeMusica", valor);
        PlayerPrefs.SetFloat(KEY_VOL_MUSICA, valor);
        PlayerPrefs.Save();
    }

    public void SetVolumenSonidos(float valor)
    {
        VolumenSonidos = valor;
        AplicarAudio("VolumeSonidos", valor);
        PlayerPrefs.SetFloat(KEY_VOL_SONIDOS, valor);
        PlayerPrefs.Save();
    }

    private void AplicarAudio(string parametroMixer, float valorLinear)
    {
        if (audioMixer == null) return;
        float dB = Mathf.Log10(Mathf.Clamp(valorLinear, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(parametroMixer, dB);
    }
}