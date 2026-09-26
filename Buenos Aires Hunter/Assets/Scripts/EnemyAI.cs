using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemyAI : NetworkBehaviour
{
    [Header("Detección")]
[SerializeField] private float detectionRange = 25f;
[SerializeField] private float loseTargetRange = 40f;

    [Header("Movimiento")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    private float tiempoUltimoAtaque = -Mathf.Infinity;

    private NavMeshAgent agent;
    private Transform target;
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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        
        if (!IsServer)
        {
            agent.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsServer)
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
    private void EstadoIdle()
    {
        if (target == null)
        {
            FindTarget();

            if (target != null)
            {
                estadoActual = EstadoIA.Perseguir;
            }
        }
    }
    private void EstadoPerseguir()
    {
        if (target == null)
        {
            estadoActual = EstadoIA.Idle;
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > loseTargetRange)
        {
            target = null;
            agent.ResetPath();

            estadoActual = EstadoIA.Idle;
            return;
        }

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
        if (target == null)
        {
            estadoActual = EstadoIA.Idle;
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance > loseTargetRange)
        {
            target = null;
            estadoActual = EstadoIA.Idle;
            return;
        }

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

        float distanciaMasCercana = detectionRange;
        Transform jugadorMasCercano = null;

        foreach (GameObject player in players)
        {
            float distancia = Vector3.Distance(
                transform.position,
                player.transform.position
            );

            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                jugadorMasCercano = player.transform;
            }
        }

        target = jugadorMasCercano;
    }
    private void Atacar()
    {
        if (Time.time < tiempoUltimoAtaque + attackCooldown)
            return;

        tiempoUltimoAtaque = Time.time;

        Debug.Log("El enemigo ataca");
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