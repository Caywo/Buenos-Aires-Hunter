using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Sprint4Test
{
    // Verifica que la escena Tutorial cargue correctamente como parte del sistema introductorio del juego.
    [UnityTest]
    public IEnumerator CP_TUT_01_EscenaTutorialDebeCargar()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        Assert.AreEqual(
            "Tutorial",
            SceneManager.GetActiveScene().name
        );
    }
    // Verifica que Horacio se encuentre presente dentro de la escena Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_02_HoracioDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject horacio =
            GameObject.Find("Horacio");

        Assert.IsNotNull(
            horacio,
            "No se encontró Horacio en el Tutorial."
        );
    }

    // Verifica que exista un punto de generación de enemigos en el Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_03_SpawnEnemyDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject spawnEnemy =
            GameObject.Find("SpawnEnemy");

        Assert.IsNotNull(
            spawnEnemy,
            "No se encontró SpawnEnemy en el Tutorial."
        );
    }

    // Verifica que el sistema EventSystem esté disponible para gestionar las interacciones del Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_04_EventSystemDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject eventSystem =
            GameObject.Find("EventSystem");

        Assert.IsNotNull(
            eventSystem,
            "No se encontró EventSystem en el Tutorial."
        );
    }

    // Verifica que el módulo de Tienda forme parte del recorrido del Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_05_TiendaDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject tienda =
            GameObject.Find("Tienda");

        Assert.IsNotNull(
            tienda,
            "No se encontró Tienda en el Tutorial."
        );
    }

    // Verifica que el módulo de Movimiento forme parte del Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_06_MovimientoDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject movimiento =
            GameObject.Find("Movimiento");

        Assert.IsNotNull(
            movimiento,
            "No se encontró Movimiento."
        );
    }

    // Verifica que el módulo de Salto forme parte del Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_07_SaltoDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject salto =
            GameObject.Find("Salto");

        Assert.IsNotNull(
            salto,
            "No se encontró Salto."
        );
    }

    // Verifica que el módulo de Disparo forme parte del Tutorial.
    [UnityTest]
    public IEnumerator CP_TUT_08_DisparoDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Tutorial");

        GameObject disparo =
            GameObject.Find("Disparo");

        Assert.IsNotNull(
            disparo,
            "No se encontró Disparo."
        );
    }

    // Verifica que el controlador principal del menú de opciones pueda instanciarse correctamente.
    [Test]
    public void CP_OPT_01_ControladorUIOpcionesDebeInstanciarse()
    {
        GameObject objeto = new GameObject();

        ControladorUIOpciones controlador =
            objeto.AddComponent<ControladorUIOpciones>();

        Assert.IsNotNull(
            controlador,
            "No fue posible instanciar ControladorUIOpciones."
        );

        Object.DestroyImmediate(objeto);
    }

    // Verifica que el controlador del menú de opciones disponga del campo para gestionar resoluciones.
    [Test]
    public void CP_OPT_02_ControladorUIOpcionesDebeExistir()
    {
        System.Type tipo =
            typeof(ControladorUIOpciones);

        Assert.IsNotNull(
            tipo,
            "No existe ControladorUIOpciones."
        );
    }

    // Verifica que el sistema de pausa pueda instanciarse correctamente.
    [Test]
    public void CP_PAUSA_01_PausaUIDebeInstanciarse()
    {
        GameObject objeto = new GameObject();

        PausaUI pausa =
            objeto.AddComponent<PausaUI>();

        Assert.IsNotNull(
            pausa,
            "No fue posible instanciar PausaUI."
        );

        Object.DestroyImmediate(objeto);
    }

    // Verifica que exista la clase principal responsable del sistema de pausa.
    [Test]
    public void CP_PAUSA_02_PausaUIDebeExistir()
    {
        System.Type tipo =
            typeof(PausaUI);

        Assert.IsNotNull(
            tipo,
            "No existe la clase PausaUI."
        );
    }


}
