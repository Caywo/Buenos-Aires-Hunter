using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Configuracion : MonoBehaviour
{
    public static Configuracion Instance { get; private set; }

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup grupoSonidos;

    // Keys para PlayerPrefs
    private const string KEY_SENS_X = "SensibilidadX";
    private const string KEY_SENS_Y = "SensibilidadY";
    private const string KEY_VOL_MUSICA = "VolumenMusica";
    private const string KEY_VOL_SONIDOS = "VolumenSonidos";
    private const string KEY_RES_INDEX = "ResolucionIndex";

    private Resolution[] resoluciones;
    private List<Resolution> resolucionesFiltradas;
    private RefreshRate hzActual;

    public float SensibilidadX { get; private set; }
    public float SensibilidadY { get; private set; }
    public float VolumenMusica { get; private set; }
    public float VolumenSonidos { get; private set; }
    public int ResolucionIndex { get; private set; }

    public List<Resolution> ResolucionesFiltradas => resolucionesFiltradas;

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
        InicializarResoluciones();
    }

    public void CargarConfiguracion()
    {
        SensibilidadX = PlayerPrefs.GetFloat(KEY_SENS_X, 5f);
        SensibilidadY = PlayerPrefs.GetFloat(KEY_SENS_Y, 5f);
        VolumenMusica = PlayerPrefs.GetFloat(KEY_VOL_MUSICA, 0.75f);
        VolumenSonidos = PlayerPrefs.GetFloat(KEY_VOL_SONIDOS, 0.75f);
        ResolucionIndex = PlayerPrefs.GetInt(KEY_RES_INDEX, -1);

        AplicarAudio("VolumeMusica", VolumenMusica);
        AplicarAudio("VolumeSonidos", VolumenSonidos);
    }

    private void InicializarResoluciones()
    {
        resoluciones = Screen.resolutions;
        resolucionesFiltradas = new List<Resolution>();
        hzActual = Screen.currentResolution.refreshRateRatio;

        for (int i = 0; i < resoluciones.Length; i++)
        {
            if (resoluciones[i].refreshRateRatio.value == hzActual.value)
            {
                resolucionesFiltradas.Add(resoluciones[i]);
            }
        }

        if (ResolucionIndex >= 0 && ResolucionIndex < resolucionesFiltradas.Count)
        {
            SetResolution(ResolucionIndex);
        }
    }

    public void SetResolution(int indiceResolucion)
    {
        if (resolucionesFiltradas == null || indiceResolucion >= resolucionesFiltradas.Count) return;

        ResolucionIndex = indiceResolucion;
        Resolution resolucion = resolucionesFiltradas[indiceResolucion];
        Screen.SetResolution(resolucion.width, resolucion.height, FullScreenMode.FullScreenWindow, resolucion.refreshRateRatio);

        PlayerPrefs.SetInt(KEY_RES_INDEX, indiceResolucion);
        PlayerPrefs.Save();
    }

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