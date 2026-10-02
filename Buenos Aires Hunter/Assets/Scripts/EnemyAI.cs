using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(Health))]
public class EnemyAI : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float danoAtaque = 10f;

    [Header("Búsqueda de objetivo")]
    [SerializeField] private float intervaloBusqueda = 0.5f;

    private float tiempoUltimoAtaque = -Mathf.Infinity;
    private float proximaBusqueda;

    private NavMeshAgent agent;
    private Health salud;          // NUEVO: vida de este enemigo
    private Transform target;
    private Health targetHealth;   // NUEVO: vida del objetivo

    private enum EstadoIA
    {
        Idle,
        Perseguir,
        Atacar
    }

    private EstadoIA estadoActual = EstadoIA.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        salud = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            agent.enabled = false;
            return;
        }

        salud.OnMuerto += AlMorir;
    }

    public override void OnNetworkDespawn()
    {
        if (salud != null)
            salud.OnMuerto -= AlMorir;
    }

    private void AlMorir(ulong atacanteId)
    {
        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        // Un enemigo muerto no se mueve ni ataca (aunque siga en escena hasta el Despawn)
        if (salud.EstaMuerto)
            return;

        switch (estadoActual)
        {
            case EstadoIA.Idle:
                EstadoIdle();
                break;

            case EstadoIA.Perseguir:
                EstadoPerseguir();
                break;

            case EstadoIA.Atacar:
                EstadoAtacar();
                break;
        }
    }

    // NUEVO: el objetivo existe y sigue vivo
    private bool TargetValido()
    {
        return target != null && targetHealth != null && !targetHealth.EstaMuerto;
    }

    // NUEVO: suelta el objetivo y vuelve a buscar otro
    private void SoltarTarget()
    {
        target = null;
        targetHealth = null;
        estadoActual = EstadoIA.Idle;
    }

    private void EstadoIdle()
    {
        // NUEVO: no buscar en cada frame
        if (Time.time < proximaBusqueda)
            return;

        proximaBusqueda = Time.time + intervaloBusqueda;

        FindTarget();

        if (TargetValido())
        {
            estadoActual = EstadoIA.Perseguir;
        }
    }

    private void EstadoPerseguir()
    {
        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= attackRange)
        {
            agent.ResetPath();

            estadoActual = EstadoIA.Atacar;
            return;
        }

        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(target.position);

        MirarAlObjetivo();
    }

    private void EstadoAtacar()
    {
        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > attackRange)
        {
            estadoActual = EstadoIA.Perseguir;
            return;
        }

        agent.ResetPath();

        MirarAlObjetivo();
        Atacar();
    }

    private void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float distanciaMasCercana = Mathf.Infinity;
        Transform jugadorMasCercano = null;
        Health saludMasCercano = null;

        foreach (GameObject player in players)
        {
            // NUEVO: ignorar jugadores muertos
            Health h = player.GetComponentInParent<Health>();
            if (h == null || h.EstaMuerto)
                continue;

            float distancia = Vector3.Distance(
                transform.position,
                player.transform.position
            );

            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                jugadorMasCercano = player.transform;
                saludMasCercano = h;
            }
        }

        target = jugadorMasCercano;
        targetHealth = saludMasCercano;
    }

    private void Atacar()
    {
        if (Time.time < tiempoUltimoAtaque + attackCooldown)
            return;

        tiempoUltimoAtaque = Time.time;

        IDamageable objetivo = target.GetComponentInParent<IDamageable>();

        if (objetivo == null)
            return;

        objetivo.TakeDamage(
            danoAtaque,
            ReglasCombate.AtacanteNeutral
        );
    }

    private void MirarAlObjetivo()
    {
        Vector3 direccion = target.position - transform.position;
        direccion.y = 0f;

        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                10f * Time.deltaTime
            );
        }
    }
}