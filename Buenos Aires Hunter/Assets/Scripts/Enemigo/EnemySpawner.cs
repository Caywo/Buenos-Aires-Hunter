using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject enemy3Prefab;

    [Header("Enemigo 3")]
    [SerializeField] private int primeraOleadaEnemigo3 = 6;
    [SerializeField, Range(0f, 1f)] private float probabilidadEnemigo3 = 0.25f;

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

    [Header("Distancias mínimas y máximas")]
    [SerializeField] private float distanciaMinimaJugador = 15f;
    [SerializeField] private float distanciaMaximaJugador = 60f;
    [Header("Recompensa")]
    [SerializeField] private int recompensaPorKill = 100;
    [Header("Portal de aparición")]
    [SerializeField] private GameObject portalPrefab;
    [Tooltip("Segundos entre que se abre el portal y sale el enemigo.")]
    [SerializeField] private float retrasoSpawn = 1f;
    [Tooltip("Segundos que sigue abierto el portal después de que sale el enemigo.")]
    [SerializeField] private float portalAbiertoExtra = 1.5f;
    [Tooltip("El portal está centrado en su pivote: hay que subirlo para que apoye en el piso.")]
    [SerializeField] private Vector3 offsetPortal = new Vector3(0f, 2.4f, 0f);
    private int spawnsPendientes = 0;
    private int tanquesPendientes = 0;
    private List<NetworkObject> enemigosActuales =
        new List<NetworkObject>();

    private int numeroOleada = 0;

    private int enemigosGeneradosEnOleada = 0;

    private int enemigosTotalesDeOleada = 0;

    private int tanquesVivos = 0;

    private int LimiteVivos => Mathf.Max(1, Mathf.RoundToInt(maxEnemigosVivos * Dificultad.Perfil.maxEnemigosVivos));

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
        if (enemigosActuales.Count == 0 && spawnsPendientes == 0)
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

        enemigosTotalesDeOleada = Mathf.RoundToInt(
    (enemigosPrimeraOleada + ((numeroOleada - 1) * enemigosExtraPorOleada))
    * Dificultad.Perfil.cantidadEnemigos);

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
            enemigosActuales.Count + spawnsPendientes < LimiteVivos &&
            enemigosGeneradosEnOleada < enemigosTotalesDeOleada
        )
        {
            bool debeSerTanque =
                numeroOleada >= primeraOleadaTanque &&
                tanquesVivos < maxTanquesVivos &&
                PuedeGenerarTanque();

            GameObject prefab;
            bool esTanque = debeSerTanque;

            if (esTanque && tankPrefab != null)
            {
                prefab = tankPrefab;
            }
            else if (
                numeroOleada >= primeraOleadaEnemigo3 &&
                enemy3Prefab != null &&
                Random.value < probabilidadEnemigo3
            )
            {
                prefab = enemy3Prefab;
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

            if (DistanciaNoValida(punto.position))
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

        if (prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("EnemySpawner: el prefab no tiene NetworkObject.");
            return false;
        }

        enemigosGeneradosEnOleada++;
        StartCoroutine(SpawnConPortal(prefab, puntoSpawn, esTanque));

        return true;
    }

    private IEnumerator SpawnConPortal(GameObject prefab, Transform punto, bool esTanque)
    {
        spawnsPendientes++;
        if (esTanque) tanquesPendientes++;

        Vector3 posicion = punto.position;
        Quaternion rotacion = punto.rotation;

        if (portalPrefab != null)
            MostrarPortalClientRpc(posicion, rotacion, retrasoSpawn + portalAbiertoExtra);

        if (retrasoSpawn > 0f)
            yield return new WaitForSeconds(retrasoSpawn);

        spawnsPendientes--;
        if (esTanque) tanquesPendientes--;

        // Si mientras tanto se apagó el spawner o la red (por ejemplo al volver al menú), no spawnear
        if (!enabled || !IsServer || !IsSpawned)
            yield break;

        GameObject enemy = Instantiate(prefab, posicion, rotacion);
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        networkObject.Spawn(true);
        enemigosActuales.Add(networkObject);

        Debug.Log("Spawn enemigo. Oleada: " + numeroOleada +
                  " | Generados: " + enemigosGeneradosEnOleada + "/" + enemigosTotalesDeOleada +
                  " | Vivos: " + enemigosActuales.Count);
    }

    [ClientRpc]
    private void MostrarPortalClientRpc(Vector3 posicion, Quaternion rotacion, float duracion)
    {
        GameObject portal = Instantiate(
            portalPrefab,
            posicion + rotacion * offsetPortal,
            rotacion
        );

        portal.GetComponent<CrearPortal>().CerrarDespues(duracion);
    }
    private bool PuedeGenerarTanque()
    {
        if (tankPrefab == null)
            return false;

        if (tanquesVivos + tanquesPendientes >= maxTanquesVivos)
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


    private bool DistanciaNoValida(Vector3 posicion)
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

            if (distancia < distanciaMinimaJugador || distancia > distanciaMaximaJugador)
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