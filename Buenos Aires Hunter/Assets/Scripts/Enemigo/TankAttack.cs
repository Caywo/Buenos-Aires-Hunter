using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
public class TankAttack : NetworkBehaviour
{
    [Header("Embestida")]
    [SerializeField] private float distanciaActivacion = 6f;
    [SerializeField] private float tiempoPreparacion = 0.7f;
    [SerializeField] private float velocidadEmbestida = 10f;
    

    [Header("Detección de impacto")]
    [SerializeField] private float radioImpacto = 1.2f;
    [SerializeField] private float distanciaDeteccionObstaculo = 0.8f;

    [Header("Daño")]
    [SerializeField] private float danoEmbestida = 40f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 4f;

    private NavMeshAgent agent;

    private bool embistiendo;
    private bool preparando;
    private bool yaGolpeoJugador;

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
    }

    public bool PuedeEmbestir(Transform target)
    {
        if (!IsServer)
            return false;

        if (target == null)
            return false;

        if (preparando || embistiendo)
            return false;

        if (Time.time < proximoAtaque)
            return false;

        float distancia = Vector3.Distance(
            transform.position,
            target.position
        );

        return distancia <= distanciaActivacion;
    }

    public void IniciarEmbestida(Transform target)
    {
        if (!PuedeEmbestir(target))
            return;

        objetivo = target;

        direccionEmbestida =
            objetivo.position - transform.position;

        direccionEmbestida.y = 0f;

        if (direccionEmbestida.sqrMagnitude <= 0.01f)
            return;

        direccionEmbestida.Normalize();

        transform.rotation =
            Quaternion.LookRotation(direccionEmbestida);

        preparando = true;
        tiempoInicioPreparacion = Time.time;
        yaGolpeoJugador = false;

        Debug.Log("TANQUE: preparando embestida");

        agent.isStopped = true;
        agent.enabled = false;
    }

    private void Update()
    {
        if (!IsServer)
            return;

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

            Debug.Log("TANQUE: embestida iniciada");
        }
    }

    private void ActualizarEmbestida()
    {
        // Primero comprobamos si hay una pared/obstáculo.
        if (HayObstaculoDelante())
        {
            Debug.Log("TANQUE: chocó contra un obstáculo");
            TerminarEmbestida();
            return;
        }

        // Avanzamos.
        transform.position +=
            direccionEmbestida *
            velocidadEmbestida *
            Time.deltaTime;

        // Comprobamos jugadores y enemigos.
        DetectarEntidades();

        if (!embistiendo)
            return;
    }

    private void DetectarEntidades()
    {
        Collider[] impactos = Physics.OverlapSphere(
            transform.position,
            radioImpacto
        );

        foreach (Collider impacto in impactos)
        {
            Health saludGolpeada =
                impacto.GetComponentInParent<Health>();

            if (saludGolpeada == null)
                continue;

            if (saludGolpeada.EstaMuerto)
                continue;

            // No golpearnos a nosotros mismos.
            if (saludGolpeada.NetworkObjectId == NetworkObjectId)
                continue;

            // =====================================
            // JUGADOR
            // =====================================

            if (saludGolpeada.EsJugador)
            {
                if (!yaGolpeoJugador)
                {
                    saludGolpeada.TakeDamage(
                        danoEmbestida,
                        ReglasCombate.AtacanteNeutral
                    );

                    yaGolpeoJugador = true;

                    Debug.Log(
                        "TANQUE: golpeó al jugador, continúa la embestida"
                    );
                }

                continue;
            
        }

            // =====================================
            // ENEMIGO
            // =====================================

            EnemyKnockback knockback =
                impacto.GetComponentInParent<EnemyKnockback>();

            if (knockback != null)
            {
                Vector3 direccionEmpuje =
                    saludGolpeada.transform.position -
                    transform.position;

                direccionEmpuje.y = 0f;

                if (direccionEmpuje.sqrMagnitude <= 0.01f)
                    direccionEmpuje = direccionEmbestida;

                knockback.Lanzar(direccionEmpuje);

                Debug.Log(
                    "TANQUE: enemigo lanzado, continúa la embestida"
                );
            }
        }
    }

    private bool HayObstaculoDelante()
    {
        Vector3 origen =
            transform.position + Vector3.up * 0.5f;

        if (Physics.SphereCast(
            origen,
            radioImpacto * 0.7f,
            direccionEmbestida,
            out RaycastHit hit,
            distanciaDeteccionObstaculo))
        {
            Health salud =
                hit.collider.GetComponentInParent<Health>();

            // Si es jugador o enemigo, NO es un obstáculo.
            if (salud != null)
                return false;

            return true;
        }

        return false;
    }

    private void TerminarEmbestida()
    {
        embistiendo = false;
        preparando = false;
        objetivo = null;

        proximoAtaque = Time.time + cooldown;

        Debug.Log("TANQUE: embestida terminada");

        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(transform.position);
            agent.isStopped = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            radioImpacto
        );
    }
}