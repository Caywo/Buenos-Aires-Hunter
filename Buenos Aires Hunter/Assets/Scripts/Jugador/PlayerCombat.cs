using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Núcleo del combate del jugador (modelo servidor-autoritativo):
///  - El dueño predice localmente los efectos (sonido, recoil) y envía el disparo al servidor.
///  - El servidor valida (cadencia, munición, recarga, origen), hace el raycast y aplica el daño.
///  - La munición vive en NetworkLists (por slot del inventario), así NO se pierde al cambiar de arma.
/// </summary>
public class PlayerCombat : NetworkBehaviour
{
    [Header("Validación en servidor")]
    [Tooltip("Distancia máxima permitida entre el origen enviado por el cliente y la posición del jugador.")]
    [SerializeField] private float toleranciaOrigen = 3f;
    [Tooltip("El servidor acepta disparos a partir de fireRate * este factor (compensa el jitter de red).")]
    [SerializeField, Range(0.5f, 1f)] private float toleranciaCadencia = 0.85f;

    [Header("Respawn")]
    [Tooltip("En el modo duelo desactivalo y llamá a Respawnear() desde tu DuelManager.")]
    [SerializeField] private bool respawnAutomatico = true;
    [SerializeField] private float tiempoRespawn = 3f;

    // Munición por slot del inventario (mismo índice que PlayerInventory.items)
    private readonly NetworkList<int> cargador = new NetworkList<int>();
    private readonly NetworkList<int> reserva = new NetworkList<int>();
    private readonly NetworkVariable<bool> recargando = new NetworkVariable<bool>(false);

    private PlayerInventory inventario;
    private Health salud;
    private CharacterController controller;
    private Camera cam;

    // Solo servidor
    private int[] capacidadCargador;
    private int[] reservaInicial;
    private float proximoDisparoServer;
    private float finRecargaServer;
    private int slotRecargando = -1;

    // Solo dueño (predicción)
    private float proximoDisparoLocal;

    private static readonly RaycastHit[] bufferHits = new RaycastHit[16];

    public bool Recargando => recargando.Value;

    /// <summary>Se dispara en el dueño cuando el servidor confirma que su bala impactó algo dañable (hitmarker).</summary>
    public event Action OnImpactoConfirmado;

    void Awake()
    {
        inventario = GetComponent<PlayerInventory>();
        salud = GetComponent<Health>();
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        var look = GetComponent<PlayerLook>();
        cam = look != null ? look.cam : GetComponentInChildren<Camera>(true);

        recargando.OnValueChanged += AlCambiarRecargando;
        salud.OnMuerteCambiada += AlCambiarMuerte;
        inventario.OnArmaCambiada += AlCambiarArma;

        if (IsServer)
        {
            InicializarMunicion();
            salud.OnMuerto += AlMorir;
        }
    }

    public override void OnNetworkDespawn()
    {
        recargando.OnValueChanged -= AlCambiarRecargando;
        if (salud != null)
        {
            salud.OnMuerteCambiada -= AlCambiarMuerte;
            salud.OnMuerto -= AlMorir;
        }
        if (inventario != null) inventario.OnArmaCambiada -= AlCambiarArma;
    }

    void Update()
    {
        if (!IsServer) return;

        if (recargando.Value && Time.time >= finRecargaServer)
            TerminarRecarga();
    }

    // ───────────────────────── MUNICIÓN (servidor) ─────────────────────────

    private void InicializarMunicion()
    {
        int n = inventario.items.Length;
        capacidadCargador = new int[n];
        reservaInicial = new int[n];

        cargador.Clear();
        reserva.Clear();

        for (int i = 0; i < n; i++)
        {
            var data = inventario.items[i].item;
            WeaponShoot arma = (data != null && data.prefabEnMano != null)
                ? data.prefabEnMano.GetComponent<WeaponShoot>()
                : null;

            capacidadCargador[i] = arma != null ? arma.magazineSize : 0;
            reservaInicial[i] = arma != null ? arma.reserveAmmo : 0;

            cargador.Add(capacidadCargador[i]);
            reserva.Add(reservaInicial[i]);
        }
    }

    private void RestaurarMunicion()
    {
        for (int i = 0; i < cargador.Count; i++)
        {
            cargador[i] = capacidadCargador[i];
            reserva[i] = reservaInicial[i];
        }
    }

    /// <summary>Para pickups de munición (llamar desde un Interactuable en el servidor).</summary>
    public void AgregarMunicion(int slot, int cantidad)
    {
        if (!IsServer || slot < 0 || slot >= reserva.Count) return;
        reserva[slot] = reserva[slot] + cantidad;
    }

    public bool TryGetMunicionActiva(out int enCargador, out int enReserva)
    {
        int s = inventario.IndiceActivo;
        if (inventario.ArmaActual == null || s < 0 || s >= cargador.Count)
        {
            enCargador = 0;
            enReserva = 0;
            return false;
        }
        enCargador = cargador[s];
        enReserva = reserva[s];
        return true;
    }

    // ───────────────────────── DISPARO ─────────────────────────

    /// <summary>Lo llama el InputManager (solo dueño).</summary>
    public void IntentarDisparar(bool pulsacionNueva)
    {
        if (!IsOwner || salud.EstaMuerto) return;

        WeaponShoot arma = inventario.ArmaActual;
        if (arma == null) return;
        if (!arma.automatico && !pulsacionNueva) return;
        if (Time.time < proximoDisparoLocal) return;
        if (recargando.Value) return;

        int slot = inventario.IndiceActivo;
        if (slot < 0 || slot >= cargador.Count) return;

        if (cargador[slot] <= 0)
        {
            IntentarRecargar(); // cargador vacío -> recarga automática
            return;
        }

        proximoDisparoLocal = Time.time + arma.fireRate;

        // Predicción local: feedback inmediato sin esperar al servidor
        arma.PlayShootEffects(true);

        Transform c = cam.transform;
        DispararServerRpc(c.position, c.forward);
    }

    [ServerRpc]
    private void DispararServerRpc(Vector3 origen, Vector3 direccion)
    {
        if (salud.EstaMuerto || recargando.Value) return;

        WeaponShoot arma = inventario.ArmaActual;
        int slot = inventario.IndiceActivo;
        if (arma == null || slot < 0 || slot >= cargador.Count) return;
        if (cargador[slot] <= 0) return;
        if (Time.time < proximoDisparoServer) return;

        // Anti-trampa básico: el origen debe estar cerca del jugador y la dirección ser válida
        if ((origen - transform.position).sqrMagnitude > toleranciaOrigen * toleranciaOrigen) return;
        if (direccion.sqrMagnitude < 0.001f) return;
        direccion.Normalize();

        proximoDisparoServer = Time.time + arma.fireRate * toleranciaCadencia;
        cargador[slot] = cargador[slot] - 1;

        Vector3 puntoImpacto = origen + direccion * arma.range;
        Vector3 normal = -direccion;
        bool impacto = false;

        if (RaycastIgnorandoPropio(origen, direccion, arma.range, arma.hitMask, out RaycastHit hit))
        {
            impacto = true;
            puntoImpacto = hit.point;
            normal = hit.normal;

            IDamageable objetivo = hit.collider.GetComponentInParent<IDamageable>();
            if (objetivo != null)
            {
                objetivo.TakeDamage(arma.damage, OwnerClientId);
                ConfirmarImpactoClientRpc(SoloAlDueno());
            }
        }

        EfectosDisparoClientRpc(puntoImpacto, normal, impacto);
    }

    private bool RaycastIgnorandoPropio(Vector3 origen, Vector3 dir, float rango, LayerMask mask, out RaycastHit mejor)
    {
        int n = Physics.RaycastNonAlloc(origen, dir, bufferHits, rango, mask, QueryTriggerInteraction.Ignore);

        mejor = default;
        float minDist = float.MaxValue;
        bool encontrado = false;

        for (int i = 0; i < n; i++)
        {
            RaycastHit h = bufferHits[i];
            if (h.collider.transform.IsChildOf(transform)) continue; // no pegarse a uno mismo
            if (h.distance < minDist)
            {
                minDist = h.distance;
                mejor = h;
                encontrado = true;
            }
        }
        return encontrado;
    }

    [ClientRpc]
    private void EfectosDisparoClientRpc(Vector3 punto, Vector3 normal, bool impacto)
    {
        if (IsOwner) return; // el dueño ya lo predijo

        if (inventario.ArmaActual != null)
            inventario.ArmaActual.PlayShootEffects(false);

        // TODO: instanciar partículas / decal de impacto en 'punto' orientado por 'normal'
    }

    [ClientRpc]
    private void ConfirmarImpactoClientRpc(ClientRpcParams rpcParams = default)
    {
        OnImpactoConfirmado?.Invoke();
    }

    // ───────────────────────── RECARGA ─────────────────────────

    /// <summary>Lo llama el InputManager (solo dueño).</summary>
    public void IntentarRecargar()
    {
        if (!IsOwner || salud.EstaMuerto || recargando.Value) return;

        WeaponShoot arma = inventario.ArmaActual;
        int slot = inventario.IndiceActivo;
        if (arma == null || slot < 0 || slot >= cargador.Count) return;
        if (cargador[slot] >= arma.magazineSize || reserva[slot] <= 0) return;

        RecargarServerRpc();
    }

    [ServerRpc]
    private void RecargarServerRpc()
    {
        if (salud.EstaMuerto || recargando.Value) return;

        WeaponShoot arma = inventario.ArmaActual;
        int slot = inventario.IndiceActivo;
        if (arma == null || slot < 0 || slot >= cargador.Count) return;
        if (cargador[slot] >= capacidadCargador[slot] || reserva[slot] <= 0) return;

        slotRecargando = slot;
        finRecargaServer = Time.time + arma.reloadTime;
        recargando.Value = true;
    }

    private void TerminarRecarga()
    {
        int s = slotRecargando;
        recargando.Value = false;
        slotRecargando = -1;

        if (s < 0 || s >= cargador.Count) return;

        int necesarias = capacidadCargador[s] - cargador[s];
        int cargar = Mathf.Min(necesarias, reserva[s]);
        cargador[s] = cargador[s] + cargar;
        reserva[s] = reserva[s] - cargar;
    }

    private void CancelarRecarga()
    {
        if (!IsServer || !recargando.Value) return;
        recargando.Value = false;
        slotRecargando = -1;
    }

    private void AlCambiarRecargando(bool anterior, bool nuevo)
    {
        if (nuevo && inventario.ArmaActual != null)
            inventario.ArmaActual.PlayReloadEffects();
    }

    private void AlCambiarArma(WeaponShoot nuevaArma)
    {
        // Cambiar de arma cancela la recarga en curso
        CancelarRecarga();
    }

    // ───────────────────────── MUERTE Y RESPAWN ─────────────────────────

    private void AlCambiarMuerte(bool muerto)
    {
        // En todos los peers: un cadáver no bloquea balas ni se mueve
        if (controller != null) controller.enabled = !muerto;
    }

    private void AlMorir(ulong atacanteId)
    {
        CancelarRecarga();
        if (respawnAutomatico)
            StartCoroutine(RespawnTrasEspera());
    }

    private IEnumerator RespawnTrasEspera()
    {
        yield return new WaitForSeconds(tiempoRespawn);
        Respawnear();
    }

    /// <summary>SOLO SERVIDOR. Público para que un DuelManager controle las rondas.</summary>
    public void Respawnear()
    {
        if (!IsServer) return;

        Transform punto = PlayerSpawnManager.Instance != null
            ? PlayerSpawnManager.Instance.ObtenerPuntoRespawn(OwnerClientId)
            : null;

        RestaurarMunicion();
        salud.Revivir();

        if (punto != null)
            TeletransportarA(punto.position, Quaternion.Euler(0f, punto.eulerAngles.y, 0f));
    }

    /// <summary>
    /// SOLO SERVIDOR. Como el jugador usa ClientNetworkTransform (autoridad del dueño) y CharacterController,
    /// el servidor no puede mover al jugador directamente: se lo pide al dueño.
    /// </summary>
    public void TeletransportarA(Vector3 posicion, Quaternion rotacion)
    {
        if (!IsServer) return;
        TeletransportarClientRpc(posicion, rotacion, SoloAlDueno());
    }

    [ClientRpc]
    private void TeletransportarClientRpc(Vector3 posicion, Quaternion rotacion, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(posicion, rotacion);
        if (controller != null) controller.enabled = !salud.EstaMuerto;

        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(posicion, rotacion, transform.localScale);
    }

    private ClientRpcParams SoloAlDueno()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
    }
}
