
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

    [Header("Búsqueda de objetivo")]
    [SerializeField] private float intervaloBusqueda = 0.5f;

    private float proximaBusqueda;

    private NavMeshAgent agent;
    private Health salud;
    private Transform target;
    private Health targetHealth;
    private TankAttack ataqueTanque;
    private NormalAttack ataqueCuerpoACuerpo;
    private RangedAttack ataqueDistancia;
    private EnemyKnockback knockback;
    private Animator animator;

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
        ataqueTanque = GetComponent<TankAttack>();
        ataqueCuerpoACuerpo = GetComponent<NormalAttack>();
        ataqueDistancia = GetComponent<RangedAttack>();
        knockback = GetComponent<EnemyKnockback>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            if (agent != null)
                agent.enabled = false;

            return;
        }

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
        estadoActual = EstadoIA.Idle;
        target = null;
        targetHealth = null;

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Embestir");
            animator.SetTrigger("Death");
        }
    }

    private void Update()
    {
        if (!IsServer || salud.EstaMuerto)
            return;

        if (knockback != null && knockback.EstaSiendoLanzado)
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

    private bool TargetValido()
    {
        return target != null &&
               targetHealth != null &&
               !targetHealth.EstaMuerto;
    }

    private void SoltarTarget()
    {
        target = null;
        targetHealth = null;
        estadoActual = EstadoIA.Idle;
    }

    private void EstadoIdle()
    {
        if (Time.time < proximaBusqueda)
            return;

        proximaBusqueda = Time.time + intervaloBusqueda;
        FindTarget();

        if (TargetValido())
            estadoActual = EstadoIA.Perseguir;
    }

    private void EstadoPerseguir()
    {
        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        float distancia = Vector3.Distance(
            transform.position,
            target.position
        );

        // Cada tipo de enemigo utiliza su propio rango de ataque.
        float rangoActual;

        if (ataqueTanque != null)
            rangoActual = ataqueTanque.DistanciaActivacion;
        else if (ataqueDistancia != null)
            rangoActual = ataqueDistancia.DistanciaActivacion;
        else
            rangoActual = attackRange;

        if (distancia <= rangoActual)
        {
            if (agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
            }

            estadoActual = EstadoIA.Atacar;
            EstadoAtacar();
            return;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(target.position);
        }

        MirarAlObjetivo();
    }

    private void EstadoAtacar()
    {
        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        // Enemigo tirador.
        if (ataqueDistancia != null)
        {
            float distancia = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distancia > ataqueDistancia.DistanciaActivacion)
            {
                ataqueDistancia.CancelarApuntado();
                estadoActual = EstadoIA.Perseguir;
                return;
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            MirarAlObjetivo();
            ataqueDistancia.IntentarDisparar(target);
            return;
        }

        // Tanque.
        if (ataqueTanque != null)
        {
            if (ataqueTanque.EstaOcupado)
                return;

            float distancia = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distancia > ataqueTanque.DistanciaActivacion)
            {
                estadoActual = EstadoIA.Perseguir;
                return;
            }

            MirarAlObjetivo();

            if (ataqueTanque.PuedeEmbestir(target))
                ataqueTanque.IniciarEmbestida(target);

            return;
        }

        // Enemigo normal cuerpo a cuerpo.
        if (ataqueCuerpoACuerpo != null)
        {
            float distancia = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distancia > attackRange)
            {
                estadoActual = EstadoIA.Perseguir;
                return;
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            MirarAlObjetivo();
            ataqueCuerpoACuerpo.Atacar(target);
        }
    }

    private void FindTarget()
    {
        GameObject[] players =
            GameObject.FindGameObjectsWithTag("Player");

        float distanciaMasCercana = Mathf.Infinity;
        Transform jugadorMasCercano = null;
        Health saludMasCercano = null;

        foreach (GameObject player in players)
        {
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

    private void MirarAlObjetivo()
    {
        if (target == null)
            return;

        Vector3 direccion = target.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo =
                Quaternion.LookRotation(direccion);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacionObjetivo,
                10f * Time.deltaTime
            );
        }
    }
}
