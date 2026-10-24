using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EstadoDuelo
{
    Esperando = 0,
    CuentaRegresiva = 1,
    RondaEnCurso = 2,
    FinDeRonda = 3,
    FinDePartida = 4
}

/// <summary>
/// Maneja una partida de duelo 1v1: rondas, puntaje, cuenta regresiva, equipamiento fijo y fin de partida.
/// Va en la escena "Duelo" como objeto con NetworkObject (in-scene). Toda la lógica corre en el servidor;
/// los clientes solo leen el estado sincronizado (lo muestra DuelHUD).
/// Los jugadores ya existen desde el menú (se crean al conectarse); este manager los coloca
/// recién cuando todos terminaron de cargar la escena.
/// </summary>
public class DuelManager : NetworkBehaviour
{
    public static DuelManager Instance { get; private set; }

    [Header("Reglas")]
    [Tooltip("Rondas que hay que ganar para ganar el duelo (3 = mejor de 5).")]
    [SerializeField] private int rondasParaGanar = 3;
    [SerializeField] private int segundosCuentaRegresiva = 3;
    [SerializeField] private float pausaFinDeRonda = 3f;
    [SerializeField] private float pausaFinDePartida = 5f;

    [Header("Escena")]
    [Tooltip("Dos puntos de aparición: el primer jugador conectado usa el 0 y el segundo el 1.")]
    [SerializeField] private Transform[] puntosDeAparicion;
    [SerializeField] private string escenaMenu = "MenuPrincipal";

    [Header("Equipamiento fijo")]
    [Tooltip("Índices de Player Inventory > Items que tendrán ambos jugadores. El primero se equipa al empezar cada ronda. " +
             "Si queda vacío no se toca el inventario.")]
    [SerializeField] private int[] slotsDeArmas;

    // ───── Estado sincronizado (solo el servidor escribe) ─────
    private readonly NetworkVariable<int> estado = new NetworkVariable<int>((int)EstadoDuelo.Esperando);
    private readonly NetworkVariable<int> ronda = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> cuenta = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> puntajeA = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> puntajeB = new NetworkVariable<int>(0);
    private readonly NetworkVariable<ulong> idA = new NetworkVariable<ulong>(ulong.MaxValue);
    private readonly NetworkVariable<ulong> idB = new NetworkVariable<ulong>(ulong.MaxValue);
    private readonly NetworkVariable<long> ganadorRonda = new NetworkVariable<long>(-1);
    private readonly NetworkVariable<long> ganadorPartida = new NetworkVariable<long>(-1);

    // ───── Lectura para el HUD ─────
    public EstadoDuelo Estado => (EstadoDuelo)estado.Value;
    public int Ronda => ronda.Value;
    public int Cuenta => cuenta.Value;
    public long GanadorRonda => ganadorRonda.Value;
    public long GanadorPartida => ganadorPartida.Value;

    public void ObtenerPuntajes(ulong clientIdLocal, out int propio, out int rival)
    {
        bool soyA = clientIdLocal == idA.Value;
        propio = soyA ? puntajeA.Value : puntajeB.Value;
        rival = soyA ? puntajeB.Value : puntajeA.Value;
    }

    // ───── Solo servidor ─────
    private bool partidaIniciada;
    private bool rondaTerminada;
    private bool pvpActivadoPorEsteManager;
    private Coroutine flujo;

    // ───── Solo clientes ─────
    private bool saliendo;

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += AlCompletarCarga;
            NetworkManager.OnClientDisconnectCallback += AlDesconectarseAlguien;
            Health.OnCualquierMuerte += AlMorirAlguien;
        }
        else
        {
            // Si el host se va (fin de partida o abandono), el cliente vuelve al menú
            NetworkManager.OnClientDisconnectCallback += AlDesconectarseCliente;
        }
    }

    public override void OnNetworkDespawn()
    {
        Desuscribir();
    }

    public override void OnDestroy()
    {
        Desuscribir();

        // PvPActivo es estático: si no se apaga, el siguiente modo arrancaría con PvP encendido
        if (pvpActivadoPorEsteManager)
        {
            ReglasCombate.PvPActivo = false;
            pvpActivadoPorEsteManager = false;
        }

        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    private void Desuscribir()
    {
        Health.OnCualquierMuerte -= AlMorirAlguien;

        if (NetworkManager == null) return;

        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= AlCompletarCarga;

        NetworkManager.OnClientDisconnectCallback -= AlDesconectarseAlguien;
        NetworkManager.OnClientDisconnectCallback -= AlDesconectarseCliente;
    }

    // ───────────────────────── Arranque ─────────────────────────

    private void AlCompletarCarga(string nombreEscena, LoadSceneMode modo, List<ulong> completaron, List<ulong> fallaron)
    {
        if (partidaIniciada) return;
        if (nombreEscena != gameObject.scene.name) return; // solo la carga de ESTA escena

        // Si algún cliente no terminó de cargar (timeout), arrancar sería jugar con uno de los dos a medias
        if (fallaron != null && fallaron.Count > 0)
        {
            Debug.LogError($"[Duelo] {fallaron.Count} cliente(s) no terminaron de cargar la escena a tiempo: se cancela la partida.");
            partidaIniciada = true; // evita que se reintente
            VolverAlMenu();
            return;
        }

        var clientes = NetworkManager.ConnectedClientsList;
        if (clientes.Count < 2)
        {
            Debug.LogError("[Duelo] Se necesitan 2 jugadores conectados para empezar.");
            return;
        }

        if (puntosDeAparicion == null || puntosDeAparicion.Length < 2)
        {
            Debug.LogError("[Duelo] Faltan los 2 puntos de aparición en el DuelManager.");
            return;
        }

        Debug.Log($"[Duelo] Carga completa. Conectados: {clientes.Count}. " +
                  $"Con jugador ya creado: {ContarConJugador(clientes)}.");

        SpawnearJugadoresFaltantes();

        partidaIniciada = true;
        flujo = StartCoroutine(FlujoPartida());
    }

    // ───────────────────────── Flujo de la partida ─────────────────────────

    private IEnumerator FlujoPartida()
    {
        var clientes = NetworkManager.ConnectedClientsList;
        idA.Value = clientes[0].ClientId;
        idB.Value = clientes[1].ClientId;

        ReglasCombate.PvPActivo = true;
        pvpActivadoPorEsteManager = true;

        ConfigurarJugadores();

        yield return new WaitForSeconds(1.5f); // que los clientes terminen de crear a sus jugadores

        while (true)
        {
            ronda.Value++;
            PrepararRonda();
            yield return new WaitForSeconds(0.5f); // que lleguen el teletransporte y el equipamiento

            estado.Value = (int)EstadoDuelo.CuentaRegresiva;
            for (int s = segundosCuentaRegresiva; s > 0; s--)
            {
                cuenta.Value = s;
                yield return new WaitForSeconds(1f);
            }
            cuenta.Value = 0;

            // ¡Fuego!
            rondaTerminada = false;
            ganadorRonda.Value = -1;
            HabilitarJugadores(true);
            estado.Value = (int)EstadoDuelo.RondaEnCurso;

            yield return new WaitUntil(() => rondaTerminada);

            // AlMorirAlguien ya sumó el punto, cambió el estado y deshabilitó el combate
            yield return new WaitForSeconds(pausaFinDeRonda);

            if (puntajeA.Value >= rondasParaGanar || puntajeB.Value >= rondasParaGanar)
                break;
        }

        ganadorPartida.Value = puntajeA.Value > puntajeB.Value ? (long)idA.Value : (long)idB.Value;
        estado.Value = (int)EstadoDuelo.FinDePartida;

        yield return new WaitForSeconds(pausaFinDePartida);
        VolverAlMenu();
    }

    // Si los jugadores no se crearon en el menú (modo "crear al cargar la escena"), se crean acá,
    // cuando todos los clientes ya terminaron de cargar. Así ningún jugador viaja entre escenas.
    private void SpawnearJugadoresFaltantes()
    {
        GameObject prefab = NetworkManagerPersistente.Instance != null
            ? NetworkManagerPersistente.Instance.PlayerPrefabOriginal
            : null;

        var clientes = NetworkManager.ConnectedClientsList;
        for (int i = 0; i < clientes.Count; i++)
        {
            if (clientes[i].PlayerObject != null) continue; // ya existe (flujo normal)

            if (prefab == null)
            {
                Debug.LogError("[Duelo] No hay prefab de jugador para crear: revisá 'Player Prefab' en el NetworkManager.");
                return;
            }

            Debug.Log($"[Duelo] Creando el jugador del cliente {clientes[i].ClientId}.");

            Transform punto = puntosDeAparicion[i % puntosDeAparicion.Length];
            GameObject jugador = Instantiate(prefab, punto.position, Quaternion.Euler(0f, punto.eulerAngles.y, 0f));
            jugador.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientes[i].ClientId, true);
        }
    }

    private int ContarConJugador(IReadOnlyList<NetworkClient> clientes)
    {
        int n = 0;
        for (int i = 0; i < clientes.Count; i++)
            if (clientes[i].PlayerObject != null) n++;
        return n;
    }

    private void ConfigurarJugadores()
    {
        foreach (ulong id in new[] { idA.Value, idB.Value })
        {
            var obj = ObtenerJugador(id);
            if (obj == null) continue;

            var combate = obj.GetComponent<PlayerCombat>();
            if (combate != null) combate.ConfigurarRespawnAutomatico(false); // las rondas las manejo yo

            var inventario = obj.GetComponent<PlayerInventory>();
            if (inventario == null) continue;

            if (slotsDeArmas != null && slotsDeArmas.Length > 0)
                inventario.ConfigurarEquipamientoFijo(slotsDeArmas);
            else
                Debug.LogWarning("[Duelo] 'Slots De Armas' está vacío: los jugadores conservan su inventario actual.");
        }
    }

    private void PrepararRonda()
    {
        CongelarClientRpc(true);
        ConfigurarHudClientRpc();

        ulong[] ids = { idA.Value, idB.Value };
        for (int i = 0; i < ids.Length; i++)
        {
            var obj = ObtenerJugador(ids[i]);
            if (obj == null) continue;

            var combate = obj.GetComponent<PlayerCombat>();
            var inventario = obj.GetComponent<PlayerInventory>();

            if (combate != null)
            {
                combate.CombateHabilitado = false;

                Transform punto = puntosDeAparicion[i % puntosDeAparicion.Length];
                combate.PrepararParaRonda(punto.position, Quaternion.Euler(0f, punto.eulerAngles.y, 0f));
            }

            if (inventario != null && slotsDeArmas != null && slotsDeArmas.Length > 0)
                inventario.ForzarEquipar(slotsDeArmas[0]);
        }
    }

    private void HabilitarJugadores(bool habilitar)
    {
        foreach (ulong id in new[] { idA.Value, idB.Value })
        {
            var obj = ObtenerJugador(id);
            if (obj == null) continue;

            var combate = obj.GetComponent<PlayerCombat>();
            if (combate != null) combate.CombateHabilitado = habilitar;
        }

        CongelarClientRpc(!habilitar);
    }

    // ───────────────────────── Eventos del servidor ─────────────────────────

    // Health.OnCualquierMuerte: (quién murió, quién lo mató)
    private void AlMorirAlguien(Health victima, ulong atacanteId)
    {
        if (!IsServer || Estado != EstadoDuelo.RondaEnCurso || rondaTerminada) return;

        // Los enemigos del servidor tienen OwnerClientId 0, igual que el host: hay que filtrar por EsJugador
        if (!victima.EsJugador) return;

        ulong idVictima = victima.OwnerClientId;
        if (idVictima != idA.Value && idVictima != idB.Value) return;

        ulong ganador = idVictima == idA.Value ? idB.Value : idA.Value;
        TerminarRonda(ganador);
    }

    private void TerminarRonda(ulong ganador)
    {
        rondaTerminada = true;
        ganadorRonda.Value = (long)ganador;

        if (ganador == idA.Value) puntajeA.Value++;
        else puntajeB.Value++;

        estado.Value = (int)EstadoDuelo.FinDeRonda;
        HabilitarJugadores(false); // nadie sigue disparando ni moviéndose
    }

    // El rival abandonó: gana el que queda
    private void AlDesconectarseAlguien(ulong clientId)
    {
        if (!partidaIniciada || Estado == EstadoDuelo.FinDePartida) return;
        if (clientId == NetworkManager.LocalClientId) return; // el propio host cerrando

        if (clientId != idA.Value && clientId != idB.Value) return;

        if (flujo != null) StopCoroutine(flujo);

        ulong restante = clientId == idA.Value ? idB.Value : idA.Value;
        ganadorPartida.Value = (long)restante;
        estado.Value = (int)EstadoDuelo.FinDePartida;

        StartCoroutine(VolverAlMenuTrasPausa());
    }

    private IEnumerator VolverAlMenuTrasPausa()
    {
        yield return new WaitForSeconds(pausaFinDePartida);
        VolverAlMenu();
    }

    private void VolverAlMenu()
    {
        if (NetworkManagerPersistente.Instance != null)
            NetworkManagerPersistente.Instance.VolverAlMenu(escenaMenu);
        else
            Debug.LogError("[Duelo] No hay NetworkManagerPersistente para volver al menú.");
    }

    // ───────────────────────── Cliente ─────────────────────────

    private void AlDesconectarseCliente(ulong clientId)
    {
        if (IsServer || saliendo) return;
        saliendo = true;

        if (NetworkManagerPersistente.Instance != null)
            NetworkManagerPersistente.Instance.VolverAlMenu(escenaMenu);
    }

    // Cada peer ajusta el HUD de SU jugador local para el duelo (oculta lo que no aplica: SUBE, Wachin...)
    [ClientRpc]
    private void ConfigurarHudClientRpc()
    {
        var local = NetworkManager.LocalClient?.PlayerObject;
        if (local == null) return;

        var ui = local.GetComponentInChildren<PlayerUI>(true);
        if (ui != null) ui.ConfigurarModoDuelo();
    }

    // Se ejecuta en todos los peers: cada uno bloquea o libera a SU jugador local
    [ClientRpc]
    private void CongelarClientRpc(bool congelar)
    {
        var local = NetworkManager.LocalClient?.PlayerObject;
        if (local == null) return;

        var look = local.GetComponent<PlayerLook>();
        if (look != null) look.inputBloqueado = congelar;

        var motor = local.GetComponent<PlayerMotor>();
        if (motor != null) motor.inputBloqueado = congelar;

        var inventario = local.GetComponent<PlayerInventory>();
        if (inventario != null)
        {
            inventario.inputBloqueado = congelar;
            if (congelar) inventario.SetApuntando(false);
        }
    }

    private GameObject ObtenerJugador(ulong clientId)
    {
        if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient cliente) && cliente.PlayerObject != null)
            return cliente.PlayerObject.gameObject;

        return null;
    }
}
