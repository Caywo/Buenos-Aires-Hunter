using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyKnockback : NetworkBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float fuerzaHorizontal = 8f;
    [SerializeField] private float fuerzaVertical = 5f;
    [SerializeField] private float duracion = 0.7f;

    private NavMeshAgent agent;

    private bool siendoLanzado;
    private float tiempoInicio;

    private Vector3 velocidadHorizontal;
    private float velocidadVertical;

    public bool EstaSiendoLanzado => siendoLanzado;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Lanzar(Vector3 direccion)
    {
        if (!IsServer)
            return;

        if (siendoLanzado)
            return;

        direccion.y = 0f;

        if (direccion.sqrMagnitude <= 0.01f)
            return;

        direccion.Normalize();

        siendoLanzado = true;
        tiempoInicio = Time.time;

        velocidadHorizontal = direccion * fuerzaHorizontal;
        velocidadVertical = fuerzaVertical;

        agent.isStopped = true;
        agent.enabled = false;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (!siendoLanzado)
            return;

        ActualizarKnockback();
    }

    private void ActualizarKnockback()
    {
        transform.position +=
            velocidadHorizontal * Time.deltaTime;

        transform.position +=
            Vector3.up * velocidadVertical * Time.deltaTime;

        velocidadVertical +=
            Physics.gravity.y * Time.deltaTime;

        if (Time.time >= tiempoInicio + duracion)
        {
            TerminarKnockback();
        }
    }

    private void TerminarKnockback()
    {
        siendoLanzado = false;

        // Intentamos encontrar una posición válida sobre el NavMesh.
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            3f,
            NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
            agent.isStopped = false;
        }
    }
}
