using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PruebaInicialQA
{
    [UnityTest]
    public IEnumerator PruebaFrameworkFunciona()
    {
        yield return null;

        Assert.Pass();
    }
    [UnityTest]
    public IEnumerator CP_UI_01_CargaMenuPrincipal()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        Assert.AreEqual(
        "MenuPrincipal",
        SceneManager.GetActiveScene().name
        );

    }
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteCanvas()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject canvas = GameObject.Find("Canvas");

        Assert.IsNotNull(
        canvas,
        "No se encontró el Canvas principal."
        );
    }
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteCanvasMenuPrincipal()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject panel =
        GameObject.Find("CanvasMenuprincipal");

        Assert.IsNotNull(
        panel,
        "No se encontró CanvasMenuprincipal."
        );
    }
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteBotonJugar()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject boton =
            GameObject.Find("BotonJugar");

        Assert.IsNotNull(
            boton,
            "No se encontró BotonJugar."
        );
    }

}
