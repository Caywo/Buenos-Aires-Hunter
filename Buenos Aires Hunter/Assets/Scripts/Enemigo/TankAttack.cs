using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class TankAttack : NetworkBehaviour
{
    [Header("Embestida")]
    [SerializeField] private float distanciaActivacion = 6f;
    [SerializeField] private float tiempoPreparacion = 0.7f;
    [SerializeField] private float velocidadEmbestida = 10f;

    [Header("Detección de impacto")]
    [SerializeField] private LayerMask capaJugadores;   // capa "Jugador"
    [SerializeField] private LayerMask capaEnemigos;    // capa "Enemigo" (para lanzarlos)
    [SerializeField] private float margenDanio = 0.2f;  // margen extra sobre el CapsuleCollider
    [SerializeField] private float radioImpacto = 1.2f; // solo para obstáculos y gizmo
    [SerializeField] private float distanciaDeteccionObstaculo = 0.8f;

    [Header("Daño")]
    [SerializeField] private float danoEmbestida = 40f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 4f;

    private NavMeshAgent agent;
    private CapsuleCollider capsule;
    private Animator animator;
    private Health salud;

    private bool embistiendo;
    private bool preparando;

    private readonly HashSet<ulong> golpeadosEnEmbestida = new HashSet<ulong>();
    private readonly Collider[] bufferImpactos = new Collider[16];
    private Vector3 posicionInicioEmbestida;

    private float tiempoInicioPreparacion;
    private float tiempoInicioEmbestida;
    private float proximoAtaque;

    private Transform objetivo;
    private Vector3 direccionEmbestida;

    public bool EstaPreparando => preparando;
    public bool EstaEmbestiendo => embistiendo;
    public bool EstaOcupado => preparando || embistiendo;
    public float DistanciaActivacion => distanciaActivacion;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        salud = GetComponent<Health>();
    }

    private Vector3 CentroCuerpo()
    {
        return capsule != null
            ? transform.TransformPoint(capsule.center)
            : transform.position + Vector3.up * 0.5f;
    }

    public bool PuedeEmbestir(Transform target)
    {
        if (salud == null || salud.EstaMuerto)
            return false;

        if (!IsServer)
            return false;

        if (target == null)
            return false;

        if (preparando || embistiendo)
            return false;

        if (Time.time < proximoAtaque)
            return false;

        float distancia = Vector3.Distance(transform.position, target.position);
        return distancia <= distanciaActivacion;
    }

    public void IniciarEmbestida(Transform target)
    {
        if (salud == null || salud.EstaMuerto)
            return;

        if (!PuedeEmbestir(target))
            return;

        objetivo = target;

        direccionEmbestida = objetivo.position - transform.position;
        direccionEmbestida.y = 0f;

        if (direccionEmbestida.sqrMagnitude <= 0.01f)
            return;

        direccionEmbestida.Normalize();

        transform.rotation = Quaternion.LookRotation(direccionEmbestida);

        preparando = true;
        tiempoInicioPreparacion = Time.time;
        golpeadosEnEmbestida.Clear();
        posicionInicioEmbestida = transform.position;

        if (animator != null)
        {
            animator.SetBool("Embestiendo", false);
            animator.ResetTrigger("Embestir");
            animator.SetTrigger("Embestir");
        }

        Debug.Log("TANQUE: preparando embestida");

        agent.isStopped = true;
        agent.enabled = false;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (salud == null || salud.EstaMuerto)
        {
            preparando = false;
            embistiendo = false;
            return;
        }

        if (preparando)
        {
            ActualizarPreparacion();
            return;
        }

        if (embistiendo)
        {
            ActualizarEmbestida();
        }
    }

    private void ActualizarPreparacion()
    {
        if (Time.time >= tiempoInicioPreparacion + tiempoPreparacion)
        {
            preparando = false;
            embistiendo = true;

            tiempoInicioEmbestida = Time.time;

            if (animator != null)
            {
                animator.SetBool("Embestiendo", true);
            }

            Debug.Log("TANQUE: embestida iniciada");
        }
    }

    private void ActualizarEmbestida()
    {
        if (HayObstaculoDelante())
        {
            Debug.Log("TANQUE: terminó por obstáculo");
            TerminarEmbestida();
            return;
        }

        Vector3 siguiente = transform.position +
            direccionEmbestida * velocidadEmbestida * Time.deltaTime;

        // El pivote está baseOffset por encima del NavMesh: buscar desde los "pies".
        Vector3 pie = siguiente - Vector3.up * agent.baseOffset;

        if (!NavMesh.SamplePosition(pie, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
        {
            Debug.Log("TANQUE: terminó por borde de NavMesh");
            TerminarEmbestida();
            return;
        }

        siguiente.y = navHit.position.y + agent.baseOffset;
        transform.position = siguiente;
        Physics.SyncTransforms();

        DetectarEntidades();
    }

    private void DetectarEntidades()
    {
        if (capsule == null)
            return;

        // Misma forma que el CapsuleCollider del tanque (más un pequeño margen).
        Vector3 escala = transform.lossyScale;
        float radioBase = capsule.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
        float altura = Mathf.Max(capsule.height * Mathf.Abs(escala.y), radioBase * 2f);

        Vector3 centro = CentroCuerpo();
        float semialtura = Mathf.Max(0f, altura * 0.5f - radioBase);

        int cantidad = Physics.OverlapCapsuleNonAlloc(
            centro - Vector3.up * semialtura,
            centro + Vector3.up * semialtura,
            radioBase + margenDanio,
            bufferImpactos,
            capaJugadores | capaEnemigos,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < cantidad; i++)
        {
            Collider impacto = bufferImpactos[i];
            Health saludGolpeada = impacto.GetComponentInParent<Health>();

            if (saludGolpeada == null || saludGolpeada.EstaMuerto)
                continue;

            if (saludGolpeada.NetworkObjectId == NetworkObjectId)
                continue;

            // Una sola vez por embestida (jugador o enemigo).
            if (!golpeadosEnEmbestida.Add(saludGolpeada.NetworkObjectId))
                continue;

            if (saludGolpeada.EsJugador)
            {
                Debug.Log($"TANQUE: aplicando {danoEmbestida} de daño a {saludGolpeada.name}");
                saludGolpeada.TakeDamage(danoEmbestida, ReglasCombate.AtacanteNeutral);
                continue;
            }

            EnemyKnockback knockback = impacto.GetComponentInParent<EnemyKnockback>();

            if (knockback != null)
            {
                Vector3 direccionEmpuje = saludGolpeada.transform.position - transform.position;
                direccionEmpuje.y = 0f;

                if (direccionEmpuje.sqrMagnitude <= 0.01f)
                    direccionEmpuje = direccionEmbestida;

                knockback.Lanzar(direccionEmpuje);
            }
        }
    }

    private bool HayObstaculoDelante()
    {
        if (Physics.SphereCast(
            CentroCuerpo(),
            radioImpacto * 0.7f,
            direccionEmbestida,
            out RaycastHit hit,
            distanciaDeteccionObstaculo,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            // Si es jugador o enemigo (tiene Health), NO es un obstáculo.
            return hit.collider.GetComponentInParent<Health>() == null;
        }

        return false;
    }

    private void TerminarEmbestida()
    {
        embistiendo = false;
        preparando = false;
        objetivo = null;

        if (animator != null)
        {
            animator.SetBool("Embestiendo", false);
            animator.ResetTrigger("Embestir");
        }

        proximoAtaque = Time.time + cooldown;

        Debug.Log("TANQUE: embestida terminada");

        Vector3 destino = posicionInicioEmbestida - Vector3.up * agent.baseOffset;

        if (NavMesh.SamplePosition(transform.position - Vector3.up * agent.baseOffset,
            out NavMeshHit hit, 3f, NavMesh.AllAreas))
        {
            destino = hit.position;
        }

        transform.position = destino;
        agent.enabled = true;
        agent.Warp(destino); // Warp vuelve a aplicar el baseOffset
        agent.isStopped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radioImpacto);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && salud != null)
            salud.OnMuerto += AlMorir;
    }

    public override void OnNetworkDespawn()
    {
        if (salud != null)
            salud.OnMuerto -= AlMorir;

        base.OnNetworkDespawn();
    }

    private void AlMorir(ulong atacanteId)
    {
        preparando = false;
        embistiendo = false;
        objetivo = null;

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }
    }
}