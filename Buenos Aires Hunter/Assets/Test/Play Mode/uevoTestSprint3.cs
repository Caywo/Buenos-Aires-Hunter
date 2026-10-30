using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Sprint3Tests
{
    // Verifica que la estructura SaveData pueda instanciarse correctamente para almacenar datos de partida.
    [Test]
    public void CP_SAVE_01_SaveDataDebeInstanciarse()
    {
        SaveData saveData = new SaveData();

        Assert.IsNotNull(
            saveData,
            "No fue posible instanciar SaveData."
        );
    }

    // Verifica que la estructura JugadorGuardado pueda instanciarse correctamente para almacenar datos de un jugador.
    [Test]
    public void CP_SAVE_01_JugadorGuardadoDebeInstanciarse()
    {
        JugadorGuardado jugador = new JugadorGuardado();

        Assert.IsNotNull(
            jugador,
            "No fue posible instanciar JugadorGuardado."
        );
    }

    // Verifica que la estructura ItemGuardado pueda instanciarse correctamente para almacenar elementos del inventario.
    [Test]
    public void CP_SAVE_01_ItemGuardadoDebeInstanciarse()
    {
        ItemGuardado item = new ItemGuardado();

        Assert.IsNotNull(
            item,
            "No fue posible instanciar ItemGuardado."
        );
    }
    // Verifica que el componente SaveManager pueda instanciarse correctamente dentro de un GameObject.
    [Test]
    public void CP_SAVE_01_SaveManagerDebeInstanciarse()
    {
        GameObject obj = new GameObject();

        SaveManager saveManager =
            obj.AddComponent<SaveManager>();

        Assert.IsNotNull(
            saveManager,
            "No fue posible instanciar SaveManager."
        );

        Object.DestroyImmediate(obj);
    }

    // Verifica que la escena Supervivencia disponga de un sistema de generación de enemigos configurado.
    [UnityTest]
    public IEnumerator CP_AI_01_EnemySpawnerSupervivenciaDebeTenerComponente()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawnerSupervivencia");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawnerSupervivencia."
        );

        EnemySpawnerSupervivencia componente =
            enemySpawner.GetComponent<EnemySpawnerSupervivencia>();

        Assert.IsNotNull(
            componente,
            "EnemySpawnerSupervivencia no tiene su componente asignado."
        );
    }


    // Verifica que SaveData permita almacenar correctamente el nombre de la siguiente escena a cargar.
    [Test]
    public void CP_LOAD_01_SaveDataDebePermitirEscenaSiguiente()
    {
        SaveData saveData = new SaveData();

        saveData.escenaSiguiente = "Extraccion2";

        Assert.AreEqual(
            "Extraccion2",
            saveData.escenaSiguiente,
            "No se pudo almacenar correctamente la escena siguiente."
        );
    }

    // Verifica que SaveData permita almacenar correctamente una colección de jugadores guardados.
    [Test]
    public void CP_SAVE_01_SaveDataDebePermitirAsignarJugadores()
    {
        SaveData saveData = new SaveData();

        saveData.jugadores = new JugadorGuardado[2];

        Assert.IsNotNull(
            saveData.jugadores,
            "No fue posible asignar jugadores a SaveData."
        );
    }

    // Verifica que JugadorGuardado permita almacenar el saldo de la SUBE utilizado durante la partida.
    [Test]
    public void CP_SAVE_01_JugadorGuardadoDebePermitirGuardarSaldo()
    {
        JugadorGuardado jugador = new JugadorGuardado();

        jugador.saldoSube = 500;

        Assert.AreEqual(
            500,
            jugador.saldoSube,
            "No se pudo almacenar correctamente el saldo."
        );
    }

    // Verifica que ItemGuardado permita almacenar correctamente cantidad e índice de inventario.
    [Test]
    public void CP_SAVE_01_ItemGuardadoDebePermitirGuardarDatos()
    {
        ItemGuardado item = new ItemGuardado();

        item.indiceSlot = 2;
        item.cantidad = 5;

        Assert.AreEqual(2, item.indiceSlot);
        Assert.AreEqual(5, item.cantidad);
    }

    // Verifica que el enumerador EstadoDuelo contenga el estado Esperando utilizado al iniciar una partida.
    [Test]
    public void CP_DUEL_01_EstadoEsperandoDebeExistir()
    {
        EstadoDuelo estado = EstadoDuelo.Esperando;

        Assert.AreEqual(
            EstadoDuelo.Esperando,
            estado,
            "No se encontró el estado Esperando."
        );
    }

    // Verifica que el enumerador EstadoDuelo contenga el estado RondaEnCurso utilizado durante una ronda activa.
    [Test]
    public void CP_DUEL_01_EstadoRondaEnCursoDebeExistir()
    {
        EstadoDuelo estado = EstadoDuelo.RondaEnCurso;

        Assert.AreEqual(
            EstadoDuelo.RondaEnCurso,
            estado,
            "No se encontró el estado RondaEnCurso."
        );
    }

    // Verifica que el enumerador EstadoDuelo contenga el estado FinDePartida utilizado al finalizar el duelo.
    [Test]
    public void CP_DUEL_01_EstadoFinDePartidaDebeExistir()
    {
        EstadoDuelo estado = EstadoDuelo.FinDePartida;

        Assert.AreEqual(
            EstadoDuelo.FinDePartida,
            estado,
            "No se encontró el estado FinDePartida."
        );
    }

    // Verifica la existencia del sistema de generación de enemigos para el modo supervivencia.
    [UnityTest]
    public IEnumerator CP_AI_01_EnemySpawnerSupervivenciaDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawnerSupervivencia");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawnerSupervivencia."
        );
    }

    // Verifica la existencia del DuelManager encargado de controlar las rondas y la lógica del modo Duelo.
    [UnityTest]
    public IEnumerator CP_DUEL_01_DuelManagerDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Duelo1");

        GameObject duelManager =
            GameObject.Find("DuelManager");

        Assert.IsNotNull(
            duelManager,
            "No se encontró DuelManager."
        );
    }

    // Verifica la existencia del punto de aparición correspondiente al jugador 1 en el modo Duelo.
    [UnityTest]
    public IEnumerator CP_DUEL_01_SpawnDuelo1DebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Duelo1");

        GameObject spawn =
            GameObject.Find("Spawn1");

        Assert.IsNotNull(
            spawn,
            "No se encontró Spawn_Duelo_1."
        );
    }

    // Verifica la existencia del punto de aparición correspondiente al jugador 2 en el modo Duelo.
    [UnityTest]
    public IEnumerator CP_DUEL_01_SpawnDuelo2DebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Duelo1");

        GameObject spawn =
            GameObject.Find("Spawn2");

        Assert.IsNotNull(
            spawn,
            "No se encontró Spawn_Duelo_2."
        );
    }

    // Verifica que la escena Duelo disponga de un Canvas para mostrar la información de la partida.
    [UnityTest]
    public IEnumerator CP_DUEL_01_CanvasDueloDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Duelo1");

        GameObject canvas =
            GameObject.Find("Canvas");

        Assert.IsNotNull(
            canvas,
            "No se encontró Canvas en la escena Duelo."
        );
    }

    // Verifica que la escena Duelo disponga del sistema HUD utilizado durante la partida.
    [UnityTest]
    public IEnumerator CP_DUEL_01_HUDDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Duelo1");

        GameObject hud =
            GameObject.Find("HUD");

        Assert.IsNotNull(
            hud,
            "No se encontró HUD en la escena Duelo."
        );
    }
    // Verifica que la escena Supervivencia disponga de un EventSystem para gestionar la interfaz de usuario.
    [UnityTest]
    public IEnumerator CP_SURV_01_EventSystemDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

        GameObject eventSystem =
            GameObject.Find("EventSystem");

        Assert.IsNotNull(
            eventSystem,
            "No se encontró EventSystem."
        );
    }

    // Verifica que la escena Supervivencia disponga de un HUD para mostrar la información del jugador.
    [UnityTest]
    public IEnumerator CP_SURV_01_HUDDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

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

    // Verifica que el NPC Horacio se encuentre presente en la escena Supervivencia.
    [UnityTest]
    public IEnumerator CP_SURV_01_HoracioDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

        GameObject horacio =
            GameObject.Find("Horacio");

        Assert.IsNotNull(
            horacio,
            "No se encontró Horacio."
        );
    }

    // Verifica que el NPC Horacio disponga del componente Tienda para permitir la interacción comercial.
    [UnityTest]
    public IEnumerator CP_SURV_01_HoracioDebeTenerComponenteTienda()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

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

    // Verifica que la escena Supervivencia disponga del sistema de generación de enemigos.
    [UnityTest]
    public IEnumerator CP_SURV_01_EnemySpawnerSupervivenciaDebeExistirComoObjeto()
    {
        yield return SceneManager.LoadSceneAsync("Supervivencia");

        GameObject enemySpawner =
            GameObject.Find("EnemySpawnerSupervivencia");

        Assert.IsNotNull(
            enemySpawner,
            "No se encontró EnemySpawnerSupervivencia."
        );
    }



}