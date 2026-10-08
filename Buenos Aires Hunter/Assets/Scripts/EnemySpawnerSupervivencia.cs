using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnerSupervivencia : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject tankPrefab;

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Rondas")]
    [SerializeField] private int enemigosPrimeraRonda = 3;
    [SerializeField] private int enemigosExtraPorRonda = 1;
    [SerializeField] private float tiempoEntreRondas = 5f;

    [Header("Tienda")]
    [SerializeField] private int rondaTienda = 5;
    [SerializeField] private float tiempoDeCompra = 20f;

    [Header("Tanque")]
    [SerializeField] private int primeraRondaTanque = 4;
    [SerializeField] private int maxTanquesVivos = 1;

    [Header("Distancia de seguridad")]
    [SerializeField] private float distanciaMinimaJugador = 15f;

    [Header("Recompensa")]
    [SerializeField] private int recompensaPorKill = 100;

    public NetworkVariable<int> rondaActual = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> rondaEnPausa = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(0f,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<NetworkObject> enemigosActuales = new List<NetworkObject>();
    private int tanquesVivos = 0;
    private bool esperandoNuevaRonda = false;
    private Coroutine coroutineRonda;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        Health.OnCualquierMuerte += AlMorirAlgo;
        IniciarNuevaRonda();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        Health.OnCualquierMuerte -= AlMorirAlgo;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (rondaEnPausa.Value || esperandoNuevaRonda)
            return;

        LimpiarEnemigosActuales();

        if (enemigosActuales.Count == 0)
        {
            IniciarEsperaNuevaRonda();
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

    private void IniciarNuevaRonda()
    {
        rondaActual.Value++;

        int cantidadTotal = enemigosPrimeraRonda + ((rondaActual.Value - 1) * enemigosExtraPorRonda);

        tanquesVivos = 0;

        Debug.Log(
            "Comenzando ronda " +
            rondaActual.Value +
            " con " +
            cantidadTotal +
            " enemigos."
        );

        GenerarRondaCompleta(cantidadTotal);
    }

    private void GenerarRondaCompleta(int cantidadTotal)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemySpawnerSupervivencia: no hay Spawn Points asignados.");
            return;
        }

        for (int i = 0; i < cantidadTotal; i++)
        {
            bool debeSerTanque =
                rondaActual.Value >= primeraRondaTanque &&
                tanquesVivos < maxTanquesVivos &&
                tankPrefab != null;

            GameObject prefab = debeSerTanque ? tankPrefab : enemyPrefab;

            if (prefab == null)
            {
                Debug.LogError("EnemySpawnerSupervivencia: falta asignar un prefab de enemigo.");
                continue;
            }

            if (Spawnear(prefab, debeSerTanque) && debeSerTanque)
            {
                tanquesVivos++;
            }
        }
    }

    private bool Spawnear(GameObject prefab, bool esTanque)
    {
        List<Transform> puntosValidos = new List<Transform>();

        foreach (Transform punto in spawnPoints)
        {
            if (punto == null)
                continue;

            if (EstaDemasiadoCercaDeJugador(punto.position))
                continue;

            puntosValidos.Add(punto);
        }

        // Si todos los puntos están "sucios" por cercanía del jugador, igual
        // dejamos spawnear usando todos los puntos (si no, nunca se completaría la ronda).
        if (puntosValidos.Count == 0)
        {
            puntosValidos.AddRange(spawnPoints);
        }

        if (puntosValidos.Count == 0)
            return false;

        Transform puntoSpawn = puntosValidos[Random.Range(0, puntosValidos.Count)];

        GameObject enemy = Instantiate(prefab, puntoSpawn.position, puntoSpawn.rotation);
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("EnemySpawnerSupervivencia: el prefab no tiene NetworkObject.");
            Destroy(enemy);
            return false;
        }

        networkObject.Spawn();
        enemigosActuales.Add(networkObject);

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

        foreach (NetworkClient cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject == null)
                continue;

            float distancia = Vector3.Distance(posicion, cliente.PlayerObject.transform.position);

            if (distancia < distanciaMinimaJugador)
                return true;
        }

        return false;
    }

    private void IniciarEsperaNuevaRonda()
    {
        if (esperandoNuevaRonda || coroutineRonda != null)
            return;

        coroutineRonda = StartCoroutine(EsperarNuevaRonda());
    }

    private IEnumerator EsperarNuevaRonda()
    {
        esperandoNuevaRonda = true;

        bool esRondaDeTienda = rondaActual.Value % rondaTienda == 0;
        float espera = esRondaDeTienda ? tiempoDeCompra : tiempoEntreRondas;

        if (esRondaDeTienda)
        {
            Debug.Log("Ronda " + rondaActual.Value + " eliminada. Pausa de tienda por " + tiempoDeCompra + " segundos.");
            rondaEnPausa.Value = true;
        }

        tiempoRestante.Value = espera;

        while (tiempoRestante.Value > 0f)
        {
            yield return null;
            tiempoRestante.Value -= Time.deltaTime;
        }

        tiempoRestante.Value = 0f;

        if (esRondaDeTienda)
        {
            rondaEnPausa.Value = false;
        }

        esperandoNuevaRonda = false;
        coroutineRonda = null;
        IniciarNuevaRonda();
    }

    private void AlMorirAlgo(Health health, ulong atacanteId)
    {
        if (!IsServer)
            return;

        if (health.EsJugador)
            return; // el manejo de jugador caído/game over va en otro script, como vimos con PlayerDownState

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