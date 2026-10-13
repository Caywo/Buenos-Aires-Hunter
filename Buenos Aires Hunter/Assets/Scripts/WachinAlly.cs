using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NavMeshAgent))]
public class WachinAlly : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float distanciaExplosion = 1.5f;

    [Header("Búsqueda")]
    [SerializeField] private float distanciaBusqueda = 40f;
    [SerializeField] private float intervaloBusqueda = 0.5f;

    [Header("Explosión")]
    [SerializeField] private float radioExplosion = 4f;
    [SerializeField] private float danoExplosion = 50f;

    [Header("Knockback")]
    [SerializeField] private float fuerzaKnockback = 10f;

    [Header("Seguridad")]
    [SerializeField] private float tiempoMaximoVida = 20f;

    private NavMeshAgent agent;

    private Health objetivo;
    private float proximaBusqueda;
    private float tiempoSpawn;

    private bool explotando;

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
            return;
        }

        agent.enabled = true;
        agent.speed = velocidad;
        agent.stoppingDistance = 0.5f;

        tiempoSpawn = Time.time;

        BuscarObjetivo();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (explotando)
            return;

        if (Time.time >= tiempoSpawn + tiempoMaximoVida)
        {
            Desaparecer();
            return;
        }

        if (objetivo == null || objetivo.EstaMuerto)
        {
            if (Time.time >= proximaBusqueda)
            {
                BuscarObjetivo();
                proximaBusqueda = Time.time + intervaloBusqueda;
            }

            return;
        }

        PerseguirObjetivo();
    }

    private void BuscarObjetivo()
    {
        Health[] objetivos =
            FindObjectsByType<Health>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        Health mejorObjetivo = null;
        float menorDistancia = distanciaBusqueda;

        foreach (Health candidato in objetivos)
        {
            if (candidato == null)
                continue;

            if (candidato.EstaMuerto)
                continue;

            // El enano solamente busca enemigos.
            if (candidato.EsJugador)
                continue;

            float distancia =
                Vector3.Distance(
                    transform.position,
                    candidato.transform.position
                );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                mejorObjetivo = candidato;
            }
        }

        objetivo = mejorObjetivo;

        if (objetivo != null && agent.isOnNavMesh)
        {
            agent.SetDestination(
                objetivo.transform.position
            );
        }
    }

    private void PerseguirObjetivo()
    {
        if (objetivo == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        float distancia =
            Vector3.Distance(
                transform.position,
                objetivo.transform.position
            );

        if (distancia <= distanciaExplosion)
        {
            Explotar();
            return;
        }

        agent.SetDestination(
            objetivo.transform.position
        );
    }

    private void Explotar()
    {
        if (explotando)
            return;

        explotando = true;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        Collider[] objetos =
            Physics.OverlapSphere(
                transform.position,
                radioExplosion
            );

        HashSet<Health> objetivosGolpeados =
            new HashSet<Health>();

        foreach (Collider col in objetos)
        {
            if (col == null)
                continue;

            Health health =
                col.GetComponentInParent<Health>();

            if (health == null)
                continue;

            if (health.EstaMuerto)
                continue;

            // El enano no daña jugadores.
            if (health.EsJugador)
                continue;

            if (objetivosGolpeados.Contains(health))
                continue;

            objetivosGolpeados.Add(health);

            health.TakeDamage(
                danoExplosion,
                ReglasCombate.AtacanteNeutral
            );

            EnemyKnockback knockback =
                health.GetComponent<EnemyKnockback>();

            if (knockback != null)
            {
                Vector3 direccion =
                    health.transform.position -
                    transform.position;

                knockback.Lanzar(direccion);
            }
        }

        Desaparecer();
    }

    private void Desaparecer()
    {
        if (!IsServer)
            return;

        if (NetworkObject != null &&
            NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            radioExplosion
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            distanciaBusqueda
        );
    }
}