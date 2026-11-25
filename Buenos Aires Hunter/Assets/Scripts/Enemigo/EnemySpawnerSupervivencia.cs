using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class EnemySpawnerSupervivencia : NetworkBehaviour
{
    [Header("Enemigos")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject enemy3Prefab;

    [Header("Enemigo 3")]
    [SerializeField] private int primeraRondaEnemigo3 = 6;
    [SerializeField, Range(0f, 1f)] private float probabilidadEnemigo3 = 0.25f;

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
    [Header("Portal de aparición")]
    [SerializeField] private GameObject portalPrefab;
    [Tooltip("Segundos entre que se abre el portal y sale el enemigo.")]
    [SerializeField] private float retrasoSpawn = 1f;
    [Tooltip("Cada enemigo espera un tiempo extra aleatorio entre 0 y este valor, para que no salgan todos juntos.")]
    [SerializeField] private float dispersionSpawn = 2f;
    [Tooltip("Segundos que sigue abierto el portal después de que sale el enemigo.")]
    [SerializeField] private float portalAbiertoExtra = 1.5f;
    [Tooltip("El portal está centrado en su pivote: se sube para que apoye en el piso.")]
    [SerializeField] private Vector3 offsetPortal = new Vector3(0f, 2.4f, 0f);

    private int spawnsPendientes = 0;
    private readonly Dictionary<Transform, float> portalAbiertoHasta = new Dictionary<Transform, float>();

    [Header("Recompensa")]
    [SerializeField] private int recompensaPorKill = 100;

    [Header("UI")]
    public TMP_Text textoRonda;

    public NetworkVariable<int> rondaActual = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> rondaEnPausa = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<NetworkObject> enemigosActuales = new List<NetworkObject>();
    private int tanquesVivos = 0;
    private bool esperandoNuevaRonda = false;
    private bool derrotaNotificada = false;
    private Coroutine coroutineRonda;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        Health.OnCualquierMuerte += AlMorirEntidad;
        IniciarNuevaRonda();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        Health.OnCualquierMuerte -= AlMorirEntidad;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (rondaEnPausa.Value || esperandoNuevaRonda || derrotaNotificada)
            return;

        LimpiarEnemigosActuales();

        if (enemigosActuales.Count == 0 && spawnsPendientes == 0)
        {
            IniciarEsperaNuevaRonda();
        }
        ActualizarRonda();
    }

    private void ActualizarRonda()
    {
        if (textoRonda == null) return;
        textoRonda.gameObject.SetActive(true);
        if (tiempoRestante.Value > 0f)
        {
            textoRonda.text = "Próxima ronda en " + Mathf.CeilToInt(tiempoRestante.Value);
        }
        else
        {
            textoRonda.text = "Ronda " + rondaActual.Value;
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

        int cantidadTotal = Mathf.RoundToInt(
     (enemigosPrimeraRonda + ((rondaActual.Value - 1) * enemigosExtraPorRonda))
     * Dificultad.Perfil.cantidadEnemigos);

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

            GameObject prefab;

            if (debeSerTanque)
            {
                prefab = tankPrefab;
            }
            else if (
                rondaActual.Value >= primeraRondaEnemigo3 &&
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

        if (prefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("EnemySpawnerSupervivencia: el prefab no tiene NetworkObject.");
            return false;
        }

        StartCoroutine(SpawnConPortal(prefab, puntoSpawn));
        return true;
    }
    private IEnumerator SpawnConPortal(GameObject prefab, Transform punto)
    {
        spawnsPendientes++;

        Vector3 posicion = punto.position;
        Quaternion rotacion = punto.rotation;

        // Cada enemigo sale con un retraso distinto para que no aparezcan todos de golpe
        float dispersion = Random.Range(0f, dispersionSpawn);
        if (dispersion > 0f) yield return new WaitForSeconds(dispersion);

        if (portalPrefab != null && PuedeSeguirSpawneando())
        {
            // Un solo portal por punto: se reutiliza si seguirá abierto cuando el enemigo salga
            bool yaAbierto = portalAbiertoHasta.TryGetValue(punto, out float hasta)
                             && hasta > Time.time + retrasoSpawn;
            if (!yaAbierto)
            {
                float duracion = retrasoSpawn + portalAbiertoExtra;
                MostrarPortalClientRpc(posicion, rotacion, duracion);
                portalAbiertoHasta[punto] = Time.time + duracion;
            }
        }

        if (retrasoSpawn > 0f) yield return new WaitForSeconds(retrasoSpawn);

        spawnsPendientes--;

        // Si mientras tanto terminó la partida o se apagó la red, no spawnear
        if (!PuedeSeguirSpawneando()) yield break;

        GameObject enemy = Instantiate(prefab, posicion, rotacion);
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        networkObject.Spawn();
        enemigosActuales.Add(networkObject);
    }

    private bool PuedeSeguirSpawneando()
    {
        return IsServer && IsSpawned && !derrotaNotificada;
    }

    [ClientRpc]
    private void MostrarPortalClientRpc(Vector3 posicion, Quaternion rotacion, float duracion)
    {
        GameObject portal = Instantiate(
            portalPrefab,
            posicion + rotacion * offsetPortal,
            rotacion
        );

        CrearPortal crearPortal = portal.GetComponent<CrearPortal>();

        if (crearPortal != null)
            crearPortal.CerrarDespues(duracion);
        else
            Destroy(portal, duracion);
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

    private void AlMorirEntidad(Health health, ulong atacanteId)
    {
        if (!IsServer)
            return;

        if (health.EsJugador)
        {
            EvaluarDerrotaEquipo();
            return;
        }

        DarRecompensaAtacante(atacanteId);
    }

    private void EvaluarDerrotaEquipo()
    {
        if (derrotaNotificada) return;

        bool quedanJugadoresVivos = false;

        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                var health = cliente.PlayerObject.GetComponent<Health>();
                if (health != null && !health.EstaMuerto)
                {
                    quedanJugadoresVivos = true;
                    break;
                }
            }
        }

        if (!quedanJugadoresVivos)
        {
            derrotaNotificada = true;
            NotificarDerrotaEquipoClientRpc();
        }
    }

    [ClientRpc]
    private void NotificarDerrotaEquipoClientRpc()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            var ui = localPlayer.GetComponentInChildren<PlayerUI>(true);
            if (ui != null)
            {
                ui.MostrarPantallaDerrota();
            }
        }
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