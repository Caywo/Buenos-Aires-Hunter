using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Configuracion : MonoBehaviour
{

    public TMP_Dropdown dropdown;
    private Resolution[] resoluciones;
    private List<Resolution> resolucionesFiltradas;
    private RefreshRate hzActual;
    private int IndiceResolucionActual = 0;

    void Awake()
    {
        Resolution resolucion = Screen.currentResolution;

        Screen.SetResolution(
            resolucion.width,
            resolucion.height,
            FullScreenMode.FullScreenWindow,
            resolucion.refreshRateRatio
        );
    }
    void Start()
    {
        resoluciones = Screen.resolutions;
        resolucionesFiltradas = new List<Resolution>();

        dropdown.ClearOptions();
        hzActual = Screen.currentResolution.refreshRateRatio;

        for (int i = 0; i < resoluciones.Length; i++)
        {
            if (resoluciones[i].refreshRateRatio == hzActual)
            {
                resolucionesFiltradas.Add(resoluciones[i]);
            }
        }

        List<string> opciones = new List<string>();
        for (int i = 0; i < resolucionesFiltradas.Count; i++)
        {
            string opcionResolucion = resolucionesFiltradas[i].width + "x" + resolucionesFiltradas[i].height + " " + resolucionesFiltradas[i].refreshRateRatio + " Hz ";
            opciones.Add(opcionResolucion);
            if (resolucionesFiltradas[i].width == Screen.width && resolucionesFiltradas[i].height == Screen.height)
            {
                IndiceResolucionActual = i;
            }
        }

        dropdown.AddOptions(opciones);
        dropdown.value = IndiceResolucionActual;
        dropdown.RefreshShownValue();
    }

    public void SetResolution(int indiceResolucion)
    {
        Resolution resolucion = resolucionesFiltradas[indiceResolucion];
        Screen.SetResolution(resolucion.width, resolucion.height, true);
    }
}
