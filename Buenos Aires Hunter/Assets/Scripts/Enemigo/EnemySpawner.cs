using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject tankPrefab;

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Oleadas")]
    [SerializeField] private int enemigosPrimeraOleada = 3;
    [SerializeField] private int enemigosExtraPorOleada = 1;
    [SerializeField] private float tiempoEntreOleadas = 5f;

    [Header("Límite de enemigos")]
    [SerializeField] private int maxEnemigosVivos = 4;

    [Header("Tanque")]
    [SerializeField] private int primeraOleadaTanque = 4;
    [SerializeField] private int maxTanquesVivos = 1;

    [Header("Distancia de seguridad")]
    [SerializeField] private float distanciaMinimaJugador = 15f;
    [Header("Recompensa")]
    [SerializeField] private int recompensaPorKill = 100;

    private List<NetworkObject> enemigosActuales =
        new List<NetworkObject>();

    private int numeroOleada = 0;

    private int enemigosGeneradosEnOleada = 0;

    private int enemigosTotalesDeOleada = 0;

    private int tanquesVivos = 0;

    private bool esperandoNuevaOleada = false;

    private Coroutine coroutineOleada;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        Health.OnCualquierMuerte += AlMorirEnemigo;
        IniciarNuevaOleada();
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        Health.OnCualquierMuerte -= AlMorirEnemigo;
    }


    private void Update()
    {
        if (!IsServer)
            return;

        LimpiarEnemigosActuales();

        if (esperandoNuevaOleada)
            return;

        // Mientras todavía haya enemigos por generar,
        // intentamos mantener lleno el límite de enemigos vivos.
        if (enemigosGeneradosEnOleada < enemigosTotalesDeOleada)
        {
            IntentarGenerarEnemigos();
            return;
        }

        // Ya se generaron todos los enemigos de esta oleada.
        // Esperamos a que mueran todos.
        if (enemigosActuales.Count == 0)
        {
            IniciarEsperaNuevaOleada();
        }
    }


    private void LimpiarEnemigosActuales()
    {
        for (int i = enemigosActuales.Count - 1; i >= 0; i--)
        {
            NetworkObject enemigo = enemigosActuales[i];

            if (enemigo == null || !enemigo.IsSpawned)
            {
                enemigosActuales.RemoveAt(i);
            }
        }

        ActualizarCantidadTanques();
    }


    private void IniciarNuevaOleada()
    {
        numeroOleada++;

        enemigosTotalesDeOleada =
            enemigosPrimeraOleada +
            ((numeroOleada - 1) * enemigosExtraPorOleada);

        enemigosGeneradosEnOleada = 0;

        tanquesVivos = 0;

        Debug.Log(
            "Comenzando oleada " +
            numeroOleada +
            " con " +
            enemigosTotalesDeOleada +
            " enemigos."
        );

        IntentarGenerarEnemigos();
    }


    private void IntentarGenerarEnemigos()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "EnemySpawner: no hay Spawn Points asignados."
            );

            return;
        }

        while (
            enemigosActuales.Count < maxEnemigosVivos &&
            enemigosGeneradosEnOleada < enemigosTotalesDeOleada
        )
        {
            bool debeSerTanque =
                numeroOleada >= primeraOleadaTanque &&
                tanquesVivos < maxTanquesVivos &&
                PuedeGenerarTanque();

            GameObject prefab;

            if (debeSerTanque && tankPrefab != null)
            {
                prefab = tankPrefab;
            }
            else
            {
                prefab = enemyPrefab;
            }

            if (prefab == null)
            {
                Debug.LogError(
                    "EnemySpawner: falta asignar un prefab de enemigo."
                );

                return;
            }

            if (!IntentarSpawn(prefab, debeSerTanque))
            {
                // No encontramos un Spawn Point válido.
                // Esperamos al siguiente Update para volver a intentar.
                return;
            }
        }
    }


    private bool IntentarSpawn(
        GameObject prefab,
        bool esTanque)
    {
        List<Transform> puntosValidos =
            new List<Transform>();

        foreach (Transform punto in spawnPoints)
        {
            if (punto == null)
                continue;

            if (EstaDemasiadoCercaDeJugador(punto.position))
                continue;

            puntosValidos.Add(punto);
        }

        if (puntosValidos.Count == 0)
        {
            return false;
        }

        Transform puntoSpawn =
            puntosValidos[
                Random.Range(0, puntosValidos.Count)
            ];

        GameObject enemy = Instantiate(
            prefab,
            puntoSpawn.position,
            puntoSpawn.rotation
        );

        NetworkObject networkObject =
            enemy.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "EnemySpawner: el prefab no tiene NetworkObject."
            );

            Destroy(enemy);
            return false;
        }

        networkObject.Spawn();

        enemigosActuales.Add(networkObject);

        enemigosGeneradosEnOleada++;

        if (esTanque)
            tanquesVivos++;

        Debug.Log(
            "Spawn enemigo. Oleada: " +
            numeroOleada +
            " | Generados: " +
            enemigosGeneradosEnOleada +
            "/" +
            enemigosTotalesDeOleada +
            " | Vivos: " +
            enemigosActuales.Count
        );

        return true;
    }


    private bool PuedeGenerarTanque()
    {
        if (tankPrefab == null)
            return false;

        if (tanquesVivos >= maxTanquesVivos)
            return false;

        return true;
    }


    private void ActualizarCantidadTanques()
    {
        int cantidad = 0;

        foreach (NetworkObject enemigo in enemigosActuales)
        {
            if (enemigo == null || !enemigo.IsSpawned)
                continue;

            if (enemigo.GetComponent<TankAttack>() != null)
                cantidad++;
        }

        tanquesVivos = cantidad;
    }


    private bool EstaDemasiadoCercaDeJugador(Vector3 posicion)
    {
        if (NetworkManager.Singleton == null)
            return false;

        foreach (
            NetworkClient cliente
            in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject == null)
                continue;

            float distancia =
                Vector3.Distance(
                    posicion,
                    cliente.PlayerObject.transform.position
                );

            if (distancia < distanciaMinimaJugador)
                return true;
        }

        return false;
    }


    private void IniciarEsperaNuevaOleada()
    {
        if (esperandoNuevaOleada)
            return;

        if (coroutineOleada != null)
            return;

        coroutineOleada =
            StartCoroutine(EsperarNuevaOleada());
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

        yield return new WaitForSeconds(
            tiempoEntreOleadas
        );

        esperandoNuevaOleada = false;

        coroutineOleada = null;

        IniciarNuevaOleada();
    }

    private void AlMorirEnemigo(Health health, ulong atacanteId)
    {
        if (!IsServer)
            return;

        if (health.EsJugador)
            return;

        DarRecompensaAtacante(atacanteId);
    }

    private void DarRecompensaAtacante(ulong atacanteId)
    {
        if (atacanteId == ReglasCombate.AtacanteNeutral)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(atacanteId, out var cliente))
            return;

        if (cliente.PlayerObject == null)
            return;

        var inventario = cliente.PlayerObject.GetComponent<PlayerInventory>();
        inventario?.AgregarSaldo(recompensaPorKill);
    }
}