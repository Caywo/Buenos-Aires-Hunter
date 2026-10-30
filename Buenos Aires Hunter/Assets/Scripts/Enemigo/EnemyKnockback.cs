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
    [SerializeField] private float caidaMaxima = 3f; // seguro anti "bajo el mapa"

    [Header("NavMesh")]
    [SerializeField] private float radioBusquedaNavMesh = 3f;

    [Header("Colisiones durante el lanzamiento")]
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float margenColision = 0.05f;

    private NavMeshAgent agent;
    private CapsuleCollider capsule;
    private Rigidbody rb;

    private bool siendoLanzado;
    private float tiempoInicio;
    private Vector3 posicionInicial;
    private int mascaraEfectiva;
    private bool rbEraKinematico;

    private Vector3 velocidadHorizontal;
    private float velocidadVertical;

    public bool EstaSiendoLanzado => siendoLanzado;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        capsule = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();

        // Ignorar la capa propia: así otros enemigos no cuentan como obstáculo ni como suelo.
        mascaraEfectiva = capasObstaculos & ~(1 << gameObject.layer);
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
        posicionInicial = transform.position; // seguro estaba en el NavMesh

        velocidadHorizontal = direccion * fuerzaHorizontal;
        velocidadVertical = fuerzaVertical;

        agent.isStopped = true;
        agent.ResetPath();
        agent.enabled = false;

        // Que la física no lo empuje ni lo hunda mientras lo movemos a mano.
        if (rb != null)
        {
            rbEraKinematico = rb.isKinematic;
            rb.isKinematic = true;
        }
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

        // Seguro: si cayó demasiado por debajo del punto de partida, recuperarlo.
        if (transform.position.y < posicionInicial.y - caidaMaxima)
        {
            TerminarKnockback();
            return;
        }

        // Horizontal
        if (!MoverConColision(velocidadHorizontal * delta))
            velocidadHorizontal = Vector3.zero;

        // Vertical
        if (!MoverConColision(Vector3.up * velocidadVertical * delta))
        {
            if (velocidadVertical > 0f)
            {
                velocidadVertical = 0f; // golpeó un techo
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

        // Detección de suelo al descender (ignora enemigos).
        if (velocidadVertical <= 0f &&
            Physics.Raycast(
                transform.position + Vector3.up * alturaRaycast,
                Vector3.down,
                out RaycastHit suelo,
                alturaRaycast + distanciaRaycast,
                mascaraEfectiva,
                QueryTriggerInteraction.Ignore) &&
            suelo.normal.y > 0.5f)
        {
            transform.position = suelo.point;
            TerminarKnockback();
            return;
        }

        if (Time.time >= tiempoInicio + duracion)
            TerminarKnockback();
    }

    private bool MoverConColision(Vector3 desplazamiento)
    {
        if (desplazamiento.sqrMagnitude <= 0.000001f)
            return true;

        if (capsule == null || !capsule.enabled)
            return false;

        Vector3 escala = transform.lossyScale;
        float radio = capsule.radius * Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.z));
        float altura = Mathf.Max(capsule.height * Mathf.Abs(escala.y), radio * 2f);

        Vector3 centro = transform.TransformPoint(capsule.center);
        float semialtura = Mathf.Max(0f, altura * 0.5f - radio);

        Vector3 puntoSuperior = centro + Vector3.up * semialtura;
        Vector3 puntoInferior = centro - Vector3.up * semialtura;

        float distancia = desplazamiento.magnitude;
        Vector3 direccion = desplazamiento / distancia;

        if (Physics.CapsuleCast(
            puntoInferior,
            puntoSuperior,
            Mathf.Max(0.01f, radio - margenColision),
            direccion,
            out RaycastHit hit,
            distancia + margenColision,
            mascaraEfectiva,
            QueryTriggerInteraction.Ignore))
        {
            float distanciaSegura = Mathf.Max(0f, hit.distance - margenColision);
            transform.position += direccion * Mathf.Min(distancia, distanciaSegura);
            return false;
        }

        transform.position += desplazamiento;
        return true;
    }

    private bool BuscarPuntoNavMesh(Vector3 origen, out Vector3 resultado)
    {
        if (NavMesh.SamplePosition(origen, out NavMeshHit hit,
            radioBusquedaNavMesh, agent.areaMask))
        {
            resultado = hit.position;
            return true;
        }

        resultado = origen;
        return false;
    }

    private void TerminarKnockback()
    {
        // 1) cerca de donde terminó, 2) cerca de donde empezó, 3) el punto exacto de inicio.
        if (!BuscarPuntoNavMesh(transform.position, out Vector3 destino) &&
            !BuscarPuntoNavMesh(posicionInicial, out destino))
        {
            destino = posicionInicial;
        }

        transform.position = destino;

        // Nunca dejar el agente deshabilitado.
        agent.enabled = true;
        agent.Warp(destino);
        agent.isStopped = false;

        if (rb != null)
            rb.isKinematic = rbEraKinematico;

        siendoLanzado = false;
        velocidadHorizontal = Vector3.zero;
        velocidadVertical = 0f;
    }
}