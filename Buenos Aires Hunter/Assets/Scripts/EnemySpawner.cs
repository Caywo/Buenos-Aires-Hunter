using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Oleadas")]
    [SerializeField] private int enemigosPrimeraOleada = 3;
    [SerializeField] private int enemigosExtraPorOleada = 1;
    [SerializeField] private float tiempoEntreOleadas = 5f;

    private List<NetworkObject> enemigosActuales = new List<NetworkObject>();

    private int numeroOleada = 0;
    private bool esperandoNuevaOleada = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        IniciarNuevaOleada();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (esperandoNuevaOleada)
            return;

        // Elimina de la lista los enemigos que ya no existen
        enemigosActuales.RemoveAll(enemy =>
            enemy == null || !enemy.IsSpawned
        );

        // Si no queda ningún enemigo, comienza el tiempo de espera
        if (enemigosActuales.Count == 0)
        {
            StartCoroutine(EsperarNuevaOleada());
        }
    }

    private void IniciarNuevaOleada()
    {
        numeroOleada++;

        int cantidadEnemigos =
            enemigosPrimeraOleada +
            ((numeroOleada - 1) * enemigosExtraPorOleada);

        Debug.Log(
            "Comenzando oleada " +
            numeroOleada +
            " con " +
            cantidadEnemigos +
            " enemigos."
        );

        for (int i = 0; i < cantidadEnemigos; i++)
        {
            SpawnEnemy(i);
        }
    }

    private void SpawnEnemy(int indice)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No hay Spawn Points asignados.");
            return;
        }

        Transform puntoSpawn =
            spawnPoints[indice % spawnPoints.Length];

        GameObject enemy = Instantiate(
            enemyPrefab,
            puntoSpawn.position,
            puntoSpawn.rotation
        );

        NetworkObject networkObject =
            enemy.GetComponent<NetworkObject>();

        networkObject.Spawn();

        enemigosActuales.Add(networkObject);
    }

    private IEnumerator EsperarNuevaOleada()
    {
        esperandoNuevaOleada = true;

        Debug.Log(
            "Oleada " +
            numeroOleada +
            " eliminada. Nueva oleada en " +
            tiempoEntreOleadas +
            " segundos."
        );

        yield return new WaitForSeconds(tiempoEntreOleadas);

        esperandoNuevaOleada = false;

        IniciarNuevaOleada();
    }
}