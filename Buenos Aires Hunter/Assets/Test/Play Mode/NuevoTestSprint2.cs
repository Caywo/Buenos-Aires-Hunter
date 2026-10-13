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
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject hud = GameObject.Find("HUD");

        Assert.IsNotNull(
            hud,
            "No se encontró el objeto HUD."
        );
    }

    // Verifica que el texto de saldo se encuentre presente dentro del HUD.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoSaldoDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject texto = GameObject.Find("TextSaldo");

        Assert.IsNotNull(
            texto,
            "No se encontró TextSaldo."
        );
    }

    // Verifica que el texto de vida exista y pueda ser utilizado por el sistema de interfaz.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoVidaDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject texto = GameObject.Find("TextoVida");

        Assert.IsNotNull(
            texto,
            "No se encontró TextoVida."
        );
    }

    // Verifica que el texto de munición exista dentro de la interfaz de usuario.
    [UnityTest]
    public IEnumerator CP_HUD_01_TextoMunicionDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject texto = GameObject.Find("TextoMunicion");

        Assert.IsNotNull(
            texto,
            "No se encontró TextoMunicion."
        );
    }

    // Verifica que el HUD disponga correctamente del componente PlayerUI encargado de gestionar la visualización de vida, munición y saldo del jugador. 
    [Test]
    public void CP_HUD_01_PlayerUIDebeExistir()
    {
        GameObject hud = GameObject.Find("HUD");

        PlayerUI playerUI =
            hud.GetComponent<PlayerUI>();

        Assert.IsNotNull(playerUI);
    }

    // Verifica la existencia de un EnemySpawner dentro de la escena, encargado de gestionar la aparición de enemigos.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_EnemySpawnerDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawner");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawner."
        );
    }

    // Verifica que EnemySpawner disponga correctamente del componente responsable de gestionar la generación de enemigos en la escena.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_EnemySpawnerDebeTenerComponente()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

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

    // Verifica que EnemySpawner disponga de al menos un punto de aparición configurado para la generación de enemigos en la escena.
    [UnityTest]
    public IEnumerator CP_ENEMY_01_SpawnPointDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

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
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

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
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

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