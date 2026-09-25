using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemyAI : NetworkBehaviour
{
    [Header("Detección")]
    [SerializeField] private float detectionRange = 15f;

    [Header("Movimiento")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    private float tiempoUltimoAtaque = -Mathf.Infinity;

    private NavMeshAgent agent;
    private Transform target;

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

        FindTarget();

        if (target != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distance <= attackRange)
            {
                // Está suficientemente cerca para atacar
                agent.ResetPath();

                Atacar();
            }
            else if (distance <= detectionRange)
            {
                
                agent.stoppingDistance = stoppingDistance;
                agent.SetDestination(target.position);
            }
            else
            {
                
                agent.ResetPath();
            }
        }
    }

    private void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float distanciaMasCercana = Mathf.Infinity;
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
    }
}