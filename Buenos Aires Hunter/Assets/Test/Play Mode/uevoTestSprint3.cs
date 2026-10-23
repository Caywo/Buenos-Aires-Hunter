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



}