using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;

public class Sprint2Tests
{
    // Verifica la existencia del objeto principal del HUD en la escena.
    [UnityTest]
    public IEnumerator CP_HUD_01_HUDDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        bool existeHUD = false;

        foreach (var obj in objetos)
        {
            if (obj.name.Contains("HUD"))
            {
                existeHUD = true;
                break;
            }
        }

        Assert.IsTrue(
            existeHUD,
            "No se encontró ningún objeto HUD."
        );
    }

    // Verifica la existencia del indicador de saldo dentro del HUD.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoSaldoDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        bool existeDinero = false;

        foreach (var obj in objetos)
        {
            if (obj.name.Contains("Dinero"))
            {
                existeDinero = true;
                break;
            }
        }

        Assert.IsTrue(
            existeDinero,
            "No se encontró el texto Dinero."
        );
    }

    // Verifica la existencia del indicador de vida utilizado por el HUD.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoVidaDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        bool existeVida = false;

        foreach (var obj in objetos)
        {
            if (obj.name.Contains("Vida"))
            {
                existeVida = true;
                break;
            }
        }

        Assert.IsTrue(
            existeVida,
            "No se encontró el objeto Vida."
        );
    }

    // Verifica la existencia del indicador de munición dentro del HUD.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoMunicionDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        bool existeMunicion = false;

        foreach (var obj in objetos)
        {
            if (obj.name.Contains("Balas"))
            {
                existeMunicion = true;
                break;
            }
        }

        Assert.IsTrue(
            existeMunicion,
            "No se encontró el objeto Balas."
        );
    }

    // Verifica que el HUD disponga correctamente del sistema Hotbar utilizado para mostrar los elementos equipables del jugador. 
    [UnityTest]
    public IEnumerator CP_HUD_01_HotbarDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject[] objetos =
            Resources.FindObjectsOfTypeAll<GameObject>();

        bool existeHotbar = false;

        foreach (var obj in objetos)
        {
            if (obj.name.Contains("Hotbar"))
            {
                existeHotbar = true;
                break;
            }
        }

        Assert.IsTrue(
            existeHotbar,
            "No se encontró Hotbar."
        );
    }

    // Verifica que EnemySpawner disponga correctamente del componente responsable de gestionar la generación de enemigos en la escena.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_EnemySpawnerDebeTenerComponente()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawner");

        Assert.IsNotNull(enemySpawner);

        EnemySpawner componente =
            enemySpawner.GetComponent<EnemySpawner>();

        Assert.IsNotNull(
            componente,
            "EnemySpawner no tiene su componente asignado."
        );
    }

    // Verifica la existencia de un EnemySpawner dentro de la escena, encargado de gestionar la aparición de enemigos.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_EnemySpawnerDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawner");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawner."
        );
    }


    // Verifica que EnemySpawner disponga de al menos un punto de aparición configurado para la generación de enemigos en la escena.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_SpawnPointDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawner");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawner."
        );

        Assert.Greater(
            enemySpawner.transform.childCount,
            0,
            "EnemySpawner no tiene SpawnPoints asociados."
        );
    }


    // Verifica la existencia del objeto Horacio utilizado para la interacción con la tienda dentro del escenario.
    [UnityTest]
    public IEnumerator CP_NPC_STORE_01_HoracioDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject horacio =
            GameObject.Find("Horacio");

        Assert.IsNotNull(
            horacio,
            "No se encontró Horacio."
        );
    }

    // Verifica que Horacio disponga del componente Tienda utilizado para gestionar la interacción comercial.
    [UnityTest]
    public IEnumerator CP_NPC_STORE_01_HoracioDebeTenerComponenteTienda()
    {
        yield return SceneManager.LoadSceneAsync("Extraccion1");

        GameObject horacio =
            GameObject.Find("Horacio");

        Assert.IsNotNull(horacio);

        Tienda tienda =
            horacio.GetComponent<Tienda>();

        Assert.IsNotNull(
            tienda,
            "Horacio no posee componente Tienda."
        );
    }


}