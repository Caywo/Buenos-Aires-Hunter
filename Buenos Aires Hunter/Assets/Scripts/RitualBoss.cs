using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public enum EstadoRitual
{
    Inactivo,
    EnCurso,
    Invocando,
    BossActivo,
    Perdido,
    Completado
}

/// <summary>
/// Ritual del boss secreto. Va en la mesa ritual. El servidor controla el contador y la aparición del boss.
/// </summary>
public class RitualBoss : NetworkBehaviour
{
    public static RitualBoss Instancia { get; private set; }

    [Header("Ritual")]
    [SerializeField] private int sacrificiosNecesarios = 20;
    [Tooltip("Si está marcado, solo cuentan las bajas hechas por jugadores (no por aliados ni por el entorno).")]
    [SerializeField] private bool soloBajasDeJugadores = true;
    [Tooltip("Distancia máxima a la mesa para poder iniciar el ritual.")]
    [SerializeField] private float distanciaMaxima = 6f;

    [Header("Invocación")]
    [SerializeField] private GameObject bossPrefab;
    [Tooltip("Dónde aparece el boss. Si queda vacío, aparece delante de la mesa.")]
    [SerializeField] private Transform puntoInvocacion;
    [Tooltip("Segundos entre el mensaje 'algo ha despertado' y la aparición del boss.")]
    [SerializeField] private float retrasoInvocacion = 5f;
    [SerializeField] private float duracionMensajeDespertar = 4f;
    [Tooltip("Ajusta el punto de aparición al NavMesh más cercano (para bosses con NavMeshAgent).")]
    [SerializeField] private bool ajustarANavMesh = true;

    [Header("Efectos en la mesa (opcional)")]
    [Tooltip("Velas, llamas o símbolos que se encienden al iniciar el ritual (déjalo apagado en el prefab).")]
    [SerializeField] private GameObject efectoRitualActivo;
    [Tooltip("Efecto o animación de la invocación (déjalo apagado en el prefab).")]
    [SerializeField] private GameObject efectoInvocacion;
    [SerializeField] private float duracionEfectoInvocacion = 6f;
    [SerializeField] private AudioClip sonidoInicio;

    [Header("Audio del combate (opcional)")]
    [SerializeField] private AudioSource musicaBoss;
    [Tooltip("La música normal del mapa: se pausa mientras dura el combate.")]
    [SerializeField] private AudioSource musicaAmbiente;

    [Header("Textos")]
    [SerializeField] private string mensajeInicio = "EL RITUAL HA COMENZADO";
    [SerializeField] private string mensajeDespertar = "ALGO HA DESPERTADO";
    [SerializeField] private string nombreBoss = "NOMBRE DEL BOSS";
    [SerializeField] private string subtituloBoss = "";

    [Header("Recompensa")]
    [Tooltip("Saldo de la SUBE que recibe cada jugador al derrotar al boss. 0 = ninguna.")]
    [SerializeField] private int recompensaAlVencer = 0;

    private readonly NetworkVariable<EstadoRitual> estado = new NetworkVariable<EstadoRitual>(EstadoRitual.Inactivo);
    private readonly NetworkVariable<int> sacrificios = new NetworkVariable<int>(0);

    private NetworkObject bossNetworkObject;
    private Coroutine rutinaInvocacion;
    private float proximaRevision;
    private RitualUI ui;

    public EstadoRitual Estado => estado.Value;
    public int Sacrificios => sacrificios.Value;
    public int Necesarios => sacrificiosNecesarios;
    public bool PuedeIniciar => estado.Value == EstadoRitual.Inactivo;

    private RitualUI UI
    {
        get
        {
            if (ui == null)
                ui = FindAnyObjectByType<RitualUI>();

            return ui;
        }
    }

    /// <summary>Texto que se muestra al mirar la mesa.</summary>
    public string TextoInteraccion()
    {
        switch (estado.Value)
        {
            case EstadoRitual.Inactivo: return "Iniciar el ritual";
            case EstadoRitual.EnCurso: return "El ritual está en curso";
            default: return string.Empty;
        }
    }

    // ───────────────────────── CICLO DE VIDA ─────────────────────────

    public override void OnNetworkSpawn()
    {
        Instancia = this;

        if (IsServer)
            Health.OnCualquierMuerte += AlMorirAlguien;
    }

    public override void OnNetworkDespawn()
    {
        if (Instancia == this)
            Instancia = null;

        if (IsServer)
            Health.OnCualquierMuerte -= AlMorirAlguien;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (estado.Value != EstadoRitual.EnCurso && estado.Value != EstadoRitual.Invocando)
            return;

        if (Time.time < proximaRevision)
            return;

        proximaRevision = Time.time + 0.5f;

        // Si ambos jugadores mueren antes de invocar al boss, el ritual se pierde.
        if (TodosLosJugadoresMuertos())
            PerderRitual();
    }

    // ───────────────────────── INICIO (cliente → servidor) ─────────────────────────

    /// <summary>Lo llama PlayerInteract en el dueño al apretar la tecla de interactuar.</summary>
    public void IntentarIniciar()
    {
        IniciarRitualServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarRitualServerRpc(ServerRpcParams rpcParams = default)
    {
        // Solo se puede iniciar una vez por partida.
        if (estado.Value != EstadoRitual.Inactivo)
            return;

        ulong id = rpcParams.Receive.SenderClientId;

        if (!TryObtenerJugador(id, out NetworkObject jugador))
            return;

        Health salud = jugador.GetComponent<Health>();

        if (salud != null && salud.EstaMuerto)
            return;

        if (Vector3.Distance(jugador.transform.position, transform.position) > distanciaMaxima)
            return;

        sacrificios.Value = 0;
        estado.Value = EstadoRitual.EnCurso;

        RitualIniciadoClientRpc();
    }

    // ───────────────────────── PROGRESO (solo servidor) ─────────────────────────

    private void AlMorirAlguien(Health health, ulong atacanteId)
    {
        if (!IsServer || health == null)
            return;

        switch (estado.Value)
        {
            case EstadoRitual.EnCurso:
                ContarBaja(health, atacanteId);
                break;

            case EstadoRitual.BossActivo:
                if (bossNetworkObject != null && health.GetComponentInParent<NetworkObject>() == bossNetworkObject)
                    BossDerrotado();
                break;
        }
    }

    private void ContarBaja(Health health, ulong atacanteId)
    {
        if (health.EsJugador)
            return;

        // Solo enemigos (los aliados también tienen Health y no deben contar).
        if (health.GetComponent<EnemyAI>() == null)
            return;

        if (soloBajasDeJugadores && !NetworkManager.ConnectedClients.ContainsKey(atacanteId))
            return;

        if (sacrificios.Value >= sacrificiosNecesarios)
            return;

        sacrificios.Value++;

        if (sacrificios.Value >= sacrificiosNecesarios)
            ComenzarInvocacion();
    }

    private void ComenzarInvocacion()
    {
        estado.Value = EstadoRitual.Invocando;

        AlgoHaDespertadoClientRpc();

        rutinaInvocacion = StartCoroutine(InvocarTrasRetraso());
    }

    private IEnumerator InvocarTrasRetraso()
    {
        yield return new WaitForSeconds(retrasoInvocacion);

        rutinaInvocacion = null;

        // Si el ritual se perdió mientras tanto, no aparece nada.
        if (estado.Value != EstadoRitual.Invocando)
            yield break;

        InvocarBoss();
    }

    private void InvocarBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogError("[Ritual] Falta asignar el prefab del boss.");
            estado.Value = EstadoRitual.Completado;
            return;
        }

        Vector3 posicion = puntoInvocacion != null
            ? puntoInvocacion.position
            : transform.position + transform.forward * 5f;

        Quaternion rotacion = puntoInvocacion != null ? puntoInvocacion.rotation : transform.rotation;

        if (ajustarANavMesh)
        {
            if (NavMesh.SamplePosition(posicion, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                posicion = hit.position;
                Debug.Log("[Ritual] Boss colocado sobre el NavMesh.");
            }
            else
            {
                Debug.LogError("[Ritual] No se encontró NavMesh cerca del punto de invocación.");
            }
        }

        GameObject boss = Instantiate(bossPrefab, posicion, rotacion);
        NetworkObject networkObject = boss.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError("[Ritual] El prefab del boss no tiene NetworkObject.");
            Destroy(boss);
            estado.Value = EstadoRitual.Completado;
            return;
        }

        networkObject.Spawn();
        bossNetworkObject = networkObject;

        estado.Value = EstadoRitual.BossActivo;

        PresentacionBossClientRpc(nombreBoss, subtituloBoss);
    }

    private void BossDerrotado()
    {
        estado.Value = EstadoRitual.Completado;
        bossNetworkObject = null;

        if (recompensaAlVencer > 0)
        {
            foreach (NetworkClient cliente in NetworkManager.ConnectedClientsList)
            {
                if (cliente.PlayerObject == null)
                    continue;

                PlayerInventory inventario = cliente.PlayerObject.GetComponent<PlayerInventory>();

                if (inventario != null)
                    inventario.AgregarSaldo(recompensaAlVencer);
            }
        }

        BossDerrotadoClientRpc();
    }

    private void PerderRitual()
    {
        estado.Value = EstadoRitual.Perdido;

        if (rutinaInvocacion != null)
        {
            StopCoroutine(rutinaInvocacion);
            rutinaInvocacion = null;
        }

        RitualPerdidoClientRpc();
    }

    private bool TodosLosJugadoresMuertos()
    {
        bool hayJugadores = false;

        foreach (NetworkClient cliente in NetworkManager.ConnectedClientsList)
        {
            if (cliente.PlayerObject == null)
                continue;

            Health salud = cliente.PlayerObject.GetComponent<Health>();

            if (salud == null)
                continue;

            hayJugadores = true;

            if (!salud.EstaMuerto)
                return false;
        }

        return hayJugadores;
    }

    private bool TryObtenerJugador(ulong id, out NetworkObject jugador)
    {
        jugador = null;

        if (!NetworkManager.ConnectedClients.TryGetValue(id, out NetworkClient cliente))
            return false;

        jugador = cliente.PlayerObject;
        return jugador != null;
    }

    // ───────────────────────── EFECTOS (en todos los jugadores) ─────────────────────────

    [ClientRpc]
    private void RitualIniciadoClientRpc()
    {
        if (efectoRitualActivo != null)
            efectoRitualActivo.SetActive(true);

        if (sonidoInicio != null)
            AudioSource.PlayClipAtPoint(sonidoInicio, transform.position);

        RitualUI pantalla = UI;

        if (pantalla != null)
            pantalla.MostrarMensaje(mensajeInicio, $"Eliminá {sacrificiosNecesarios} enemigos", 3f);
    }

    [ClientRpc]
    private void AlgoHaDespertadoClientRpc()
    {
        RitualUI pantalla = UI;

        if (pantalla != null)
            pantalla.MostrarMensaje(mensajeDespertar, string.Empty, duracionMensajeDespertar);

        if (efectoInvocacion != null)
            StartCoroutine(MostrarEfectoInvocacion());
    }

    private IEnumerator MostrarEfectoInvocacion()
    {
        efectoInvocacion.SetActive(true);

        yield return new WaitForSeconds(duracionEfectoInvocacion);

        efectoInvocacion.SetActive(false);

        if (efectoRitualActivo != null)
            efectoRitualActivo.SetActive(false);
    }

    [ClientRpc]
    private void PresentacionBossClientRpc(string nombre, string subtitulo)
    {
        RitualUI pantalla = UI;

        if (pantalla != null)
            pantalla.MostrarMensaje(nombre, subtitulo, 3.5f);

        if (musicaAmbiente != null)
            musicaAmbiente.Pause();

        if (musicaBoss != null)
            musicaBoss.Play();
    }

    [ClientRpc]
    private void BossDerrotadoClientRpc()
    {
        if (musicaBoss != null)
            musicaBoss.Stop();

        if (musicaAmbiente != null)
            musicaAmbiente.UnPause();
    }

    [ClientRpc]
    private void RitualPerdidoClientRpc()
    {
        if (efectoRitualActivo != null)
            efectoRitualActivo.SetActive(false);

        RitualUI pantalla = UI;

        if (pantalla != null)
            pantalla.MostrarMensaje("EL RITUAL SE HA PERDIDO", string.Empty, 3f);
    }
}
