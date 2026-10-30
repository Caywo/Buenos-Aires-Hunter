using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class EnemyAI : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 2f;

    [Header("Búsqueda de objetivo")]
    [SerializeField] private float intervaloBusqueda = 0.5f;
    [Tooltip("Metros que debe estar más cerca otro jugador para cambiar de objetivo (evita oscilar).")]
    [SerializeField] private float ventajaParaCambiarObjetivo = 2f;

    [Header("Actualización de ruta")]
    [SerializeField] private float intervaloActualizacionRuta = 0.35f;
    [SerializeField] private float movimientoMinimoObjetivo = 0.75f;

    [Header("Recuperación de atasco")]
    [SerializeField] private float intervaloComprobacionAtasco = 0.5f;
    [SerializeField] private float tiempoParaConsiderarAtasco = 1.5f;
    [SerializeField] private float velocidadMinimaMovimiento = 0.15f;
    [SerializeField] private float distanciaMinimaAvance = 0.1f;

    private float proximaBusqueda;
    private float proximaActualizacionRuta;
    private float proximaComprobacionAtasco;
    private float tiempoAtascado;

    private Vector3 ultimaPosicionComprobada;
    private Vector3 ultimaPosicionObjetivoRuta;
    private bool rutaInicializada;

    private NavMeshAgent agent;
    private Health salud;
    private Transform target;
    private Health targetHealth;

    private TankAttack ataqueTanque;
    private NormalAttack ataqueCuerpoACuerpo;
    private RangedAttack ataqueDistancia;
    private EnemyKnockback knockback;
    private Animator animator;

    private static readonly int VelocidadHash = Animator.StringToHash("Velocidad");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeathHash = Animator.StringToHash("Death");

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

        if (salud != null)
            salud.OnMuerto += AlMorir;

        ReiniciarSeguimiento();
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
        ReiniciarSeguimiento();

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
            // Cada tipo de enemigo puede usar parámetros distintos.
            // No limpiar "Attack" en el tanque: su trigger es "Embestir".
            if (ataqueTanque != null)
                animator.ResetTrigger("Embestir");
            else if (ataqueCuerpoACuerpo != null)
                animator.ResetTrigger(AttackHash);

            animator.SetTrigger(DeathHash);
        }
    }

    private void Update()
    {
        if (!IsServer || salud == null || salud.EstaMuerto)
            return;

        if (knockback != null && knockback.EstaSiendoLanzado)
            return;

        // "Velocidad" se actualiza solo en el enemigo normal cuerpo a cuerpo.
        if (animator != null &&
            ataqueTanque == null &&
            ataqueDistancia == null &&
            ataqueCuerpoACuerpo != null &&
            agent != null &&
            agent.enabled)
        {
            animator.SetFloat(VelocidadHash, agent.velocity.magnitude);
        }

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
        ReiniciarSeguimiento();
    }

    private void EstadoIdle()
    {
        if (Time.time < proximaBusqueda)
            return;

        proximaBusqueda = Time.time + intervaloBusqueda;
        FindTarget();

        if (TargetValido())
        {
            estadoActual = EstadoIA.Perseguir;
            ReiniciarSeguimiento();
        }
    }

    private float ObtenerRangoAtaque()
    {
        if (ataqueTanque != null)
            return ataqueTanque.DistanciaActivacion;

        if (ataqueDistancia != null)
            return ataqueDistancia.DistanciaActivacion;

        return attackRange;
    }

    private float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void EstadoPerseguir()
    {
        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        // Con 2 jugadores, el objetivo puede cambiar si el otro se acerca más.
        ReevaluarObjetivo();

        if (!TargetValido())
        {
            SoltarTarget();
            return;
        }

        float distancia = DistanciaHorizontal(transform.position, target.position);

        if (distancia <= ObtenerRangoAtaque())
        {
            DetenerNavegacion();
            estadoActual = EstadoIA.Atacar;
            EstadoAtacar();
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;

        ActualizarRutaSiHaceFalta();
        ComprobarAtasco();
        MirarAlObjetivo();
    }

    private void ActualizarRutaSiHaceFalta()
    {
        if (target == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        bool objetivoSeMovio =
            !rutaInicializada ||
            Vector3.Distance(target.position, ultimaPosicionObjetivoRuta) >= movimientoMinimoObjetivo;

        bool tocaActualizar = Time.time >= proximaActualizacionRuta;

        if (!objetivoSeMovio && !tocaActualizar)
            return;

        agent.SetDestination(target.position);

        ultimaPosicionObjetivoRuta = target.position;
        proximaActualizacionRuta = Time.time + intervaloActualizacionRuta;

        rutaInicializada = true;
    }

    private void ComprobarAtasco()
    {
        if (Time.time < proximaComprobacionAtasco)
            return;

        proximaComprobacionAtasco = Time.time + intervaloComprobacionAtasco;

        float avance = Vector3.Distance(transform.position, ultimaPosicionComprobada);
        ultimaPosicionComprobada = transform.position;

        bool necesitaAvanzar =
            !agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance > agent.stoppingDistance + 0.5f;

        bool noAvanza =
            avance < distanciaMinimaAvance &&
            agent.velocity.magnitude < velocidadMinimaMovimiento &&
            agent.desiredVelocity.magnitude > velocidadMinimaMovimiento;

        if (necesitaAvanzar && noAvanza)
            tiempoAtascado += intervaloComprobacionAtasco;
        else
            tiempoAtascado = 0f;

        if (tiempoAtascado < tiempoParaConsiderarAtasco)
            return;

        // Recuperación: solicitar nuevamente la ruta al jugador.
        tiempoAtascado = 0f;
        rutaInicializada = false;

        if (TargetValido() && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.SetDestination(target.position);

            ultimaPosicionObjetivoRuta = target.position;
            proximaActualizacionRuta = Time.time + intervaloActualizacionRuta;

            rutaInicializada = true;
        }
    }

    private void DetenerNavegacion()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();

        ReiniciarSeguimiento();
    }

    private void ReiniciarSeguimiento()
    {
        tiempoAtascado = 0f;
        rutaInicializada = false;

        proximaActualizacionRuta = 0f;
        proximaComprobacionAtasco = Time.time + intervaloComprobacionAtasco;

        ultimaPosicionComprobada = transform.position;
        ultimaPosicionObjetivoRuta = Vector3.zero;
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
            float distancia = DistanciaHorizontal(transform.position, target.position);

            if (distancia > ataqueDistancia.DistanciaActivacion)
            {
                ataqueDistancia.CancelarApuntado();
                estadoActual = EstadoIA.Perseguir;
                ReiniciarSeguimiento();
                return;
            }

            DetenerNavegacion();
            MirarAlObjetivo();
            ataqueDistancia.IntentarDisparar(target);
            return;
        }

        // Tanque.
        if (ataqueTanque != null)
        {
            if (ataqueTanque.EstaOcupado)
                return;

            // Horizontal, igual que en Perseguir (el Base Offset del tanque falsea la distancia 3D).
            float distancia = DistanciaHorizontal(transform.position, target.position);

            if (distancia > ataqueTanque.DistanciaActivacion)
            {
                estadoActual = EstadoIA.Perseguir;
                ReiniciarSeguimiento();
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
            float distancia = DistanciaHorizontal(transform.position, target.position);

            if (distancia > attackRange)
            {
                estadoActual = EstadoIA.Perseguir;
                ReiniciarSeguimiento();
                return;
            }

            DetenerNavegacion();
            MirarAlObjetivo();
            ataqueCuerpoACuerpo.Atacar(target);
        }
    }

    // ───────────────────────── BÚSQUEDA DE OBJETIVO ─────────────────────────

    private void FindTarget()
    {
        AsignarTarget(ObjetivosJugadores.MasCercano(transform.position));
    }

    private void ReevaluarObjetivo()
    {
        if (Time.time < proximaBusqueda)
            return;

        proximaBusqueda = Time.time + intervaloBusqueda;

        Health candidato = ObjetivosJugadores.MasCercano(transform.position);

        if (candidato == null || candidato == targetHealth)
            return;

        float dActual = DistanciaHorizontal(transform.position, target.position);
        float dNuevo = DistanciaHorizontal(transform.position, candidato.transform.position);

        // Solo cambiar si el otro jugador está claramente más cerca.
        if (dNuevo + ventajaParaCambiarObjetivo < dActual)
            AsignarTarget(candidato);
    }

    private void AsignarTarget(Health nuevo)
    {
        if (nuevo == targetHealth)
            return;

        targetHealth = nuevo;
        target = nuevo != null ? nuevo.transform : null;
        rutaInicializada = false; // fuerza una ruta nueva hacia el nuevo objetivo

        if (nuevo != null)
            Debug.Log($"[IA] {name} ahora persigue a {nuevo.name}");
    }

    private void MirarAlObjetivo()
    {
        if (target == null)
            return;

        Vector3 direccion = target.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
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