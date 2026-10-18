using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyKnockback : NetworkBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float fuerzaHorizontal = 8f;
    [SerializeField] private float fuerzaVertical = 5f;
    [SerializeField] private float duracion = 0.7f;

    [Header("Caída")]
    [SerializeField] private float gravedad = -20f;
    [SerializeField] private float alturaRaycast = 0.5f;
    [SerializeField] private float distanciaRaycast = 2f;

    [Header("NavMesh")]
    [SerializeField] private float radioBusquedaNavMesh = 3f;

    [Header("Colisiones durante el lanzamiento")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float margenColision = 0.05f;

    private NavMeshAgent agent;
    private CapsuleCollider capsule;

    private bool siendoLanzado;
    private float tiempoInicio;

    private Vector3 velocidadHorizontal;
    private float velocidadVertical;

    public bool EstaSiendoLanzado => siendoLanzado;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        capsule = GetComponent<CapsuleCollider>();
    }

    public void Lanzar(Vector3 direccion)
    {
        if (!IsServer || siendoLanzado)
            return;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
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
        agent.ResetPath();
        agent.enabled = false;
    }

    private void Update()
    {
        if (!IsServer || !siendoLanzado)
            return;

        ActualizarKnockback();
    }

    private void ActualizarKnockback()
    {
        float delta = Time.deltaTime;

        // Movimiento horizontal: las paredes bloquean el desplazamiento.
        Vector3 desplazamientoHorizontal =
            velocidadHorizontal * delta;

        if (!MoverConColision(desplazamientoHorizontal))
        {
            velocidadHorizontal = Vector3.zero;
        }

        // Movimiento vertical: permite subir y caer, pero bloquea techos.
        Vector3 desplazamientoVertical =
            Vector3.up * velocidadVertical * delta;

        if (!MoverConColision(desplazamientoVertical))
        {
            if (velocidadVertical > 0f)
            {
                velocidadVertical = 0f;
            }
            else
            {
                TerminarKnockback();
                return;
            }
        }
        else
        {
            velocidadVertical += gravedad * delta;
        }

        // Detectar el suelo mientras el NPC está descendiendo.
        if (velocidadVertical <= 0f &&
            Physics.Raycast(
                transform.position + Vector3.up * alturaRaycast,
                Vector3.down,
                out RaycastHit suelo,
                alturaRaycast + distanciaRaycast,
                capasObstaculos,
                QueryTriggerInteraction.Ignore) &&
            suelo.normal.y > 0.5f)
        {
            float distanciaVertical =
                transform.position.y - suelo.point.y;

            if (distanciaVertical >= 0f &&
                distanciaVertical <= alturaRaycast + distanciaRaycast)
            {
                transform.position = suelo.point;
                TerminarKnockback();
                return;
            }
        }

        // Límite de seguridad para evitar lanzamientos infinitos.
        if (Time.time >= tiempoInicio + duracion)
        {
            TerminarKnockback();
        }
    }

    private bool MoverConColision(Vector3 desplazamiento)
    {
        if (desplazamiento.sqrMagnitude <= 0.000001f)
            return true;

        if (capsule == null || !capsule.enabled)
            return false;

        Vector3 escala = transform.lossyScale;

        float radio = capsule.radius *
            Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));

        float altura = Mathf.Max(
            capsule.height * Mathf.Abs(escala.y),
            radio * 2f);

        Vector3 centro =
            transform.TransformPoint(capsule.center);

        float semialtura =
            Mathf.Max(0f, altura * 0.5f - radio);

        Vector3 puntoSuperior =
            centro + Vector3.up * semialtura;

        Vector3 puntoInferior =
            centro - Vector3.up * semialtura;

        float distancia = desplazamiento.magnitude;
        Vector3 direccion = desplazamiento / distancia;

        if (Physics.CapsuleCast(
            puntoInferior,
            puntoSuperior,
            Mathf.Max(0.01f, radio - margenColision),
            direccion,
            out RaycastHit hit,
            distancia + margenColision,
            capasObstaculos,
            QueryTriggerInteraction.Ignore))
        {
            float distanciaSegura =
                Mathf.Max(0f, hit.distance - margenColision);

            transform.position +=
                direccion * Mathf.Min(distancia, distanciaSegura);

            return false;
        }

        transform.position += desplazamiento;
        return true;
    }

    private void TerminarKnockback()
    {
        Vector3 posicionFinal = transform.position;

        // Buscar una posición transitable cercana.
        if (!NavMesh.SamplePosition(
            posicionFinal,
            out NavMeshHit hit,
            radioBusquedaNavMesh,
            agent.areaMask))
        {
            // Intentar volver hacia la posición anterior al empujón.
            Vector3 alternativa =
                posicionFinal -
                velocidadHorizontal.normalized *
                fuerzaHorizontal * duracion;

            if (!NavMesh.SamplePosition(
                alternativa,
                out hit,
                radioBusquedaNavMesh,
                agent.areaMask))
            {
                // No reactivar el agente fuera del NavMesh.
                siendoLanzado = false;
                velocidadHorizontal = Vector3.zero;
                velocidadVertical = 0f;
                return;
            }
        }

        transform.position = hit.position;

        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
            agent.isStopped = false;
        }
        else
        {
            agent.enabled = false;
        }

        siendoLanzado = false;
        velocidadHorizontal = Vector3.zero;
        velocidadVertical = 0f;
    }
}