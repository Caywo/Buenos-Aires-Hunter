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
    private Health salud;          // NUEVO: vida de este enemigo
    private Transform target;
    private Health targetHealth;   // NUEVO: vida del objetivo
    private TankAttack ataqueTanque;
    private NormalAttack ataqueCuerpoACuerpo;
    private RangedAttack ataqueDistancia;
    private EnemyKnockback knockback;

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
        knockback = GetComponent<EnemyKnockback>();
        ataqueDistancia = GetComponent<RangedAttack>();
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

        float rangoActual = ataqueDistancia != null
            ? ataqueDistancia.DistanciaActivacion
            : attackRange;

        if (distance <= rangoActual)
        {
            agent.ResetPath();
            agent.isStopped = true;

            estadoActual = EstadoIA.Atacar;
            return;
        }

        agent.isStopped = false;

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

        // Enemigo tirador
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
            agent.isStopped = true;
            agent.ResetPath();
            MirarAlObjetivo();
            ataqueDistancia.IntentarDisparar(target);
            return;
        }

        // Tanque
        if (ataqueTanque != null)
        {
            if (ataqueTanque.EstaOcupado)
                return;

            float distancia = Vector3.Distance(
                transform.position,
                target.position
            );

            // Si está fuera del rango de embestida,
            // vuelve a perseguir al jugador.
            if (distancia > ataqueTanque.DistanciaActivacion)
            {
                estadoActual = EstadoIA.Perseguir;
                return;
            }

            if (ataqueTanque.PuedeEmbestir(target))
            {
                ataqueTanque.IniciarEmbestida(target);
                return;
            }

            MirarAlObjetivo();
        }

        // enemigo normal
        if (ataqueCuerpoACuerpo != null)
        {
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

            ataqueCuerpoACuerpo.Atacar(target);
        }
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