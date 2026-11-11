using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA del boss secreto. Fase 1: persigue y ataca. Fase 2 (a la mitad de vida): suma un ataque especial con dash.
/// Servidor autoritativo: los clientes solo ven la posición y las animaciones sincronizadas.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class BossAI : NetworkBehaviour
{
    [Header("Movimiento")]
    [Tooltip("El jugador corre a 13: el boss tiene que ser rápido para presionarlo.")]
    [SerializeField] private float velocidadFase1 = 6f;
    [SerializeField] private float velocidadFase2 = 8f;
    [SerializeField] private float velocidadGiro = 8f;
    [SerializeField] private float intervaloRuta = 0.25f;

    [Header("Ataque normal")]
    [SerializeField] private float rangoAtaque = 3f;
    [SerializeField] private float danoAtaque = 25f;
    [Tooltip("Ancho del cono del golpe, en grados.")]
    [SerializeField, Range(30f, 360f)] private float anguloAtaque = 120f;
    [Tooltip("Segundos desde que empieza la animación hasta que el golpe conecta.")]
    [SerializeField] private float retrasoGolpe = 0.5f;
    [Tooltip("Duración total de la animación de ataque.")]
    [SerializeField] private float duracionAtaque = 1.3f;
    [SerializeField] private float cooldownAtaque = 1f;

    [Header("Fase 2")]
    [Tooltip("Entra en fase 2 cuando la vida baja de esta fracción (0.5 = la mitad).")]
    [SerializeField, Range(0.05f, 0.95f)] private float fraccionVidaFase2 = 0.5f;
    [Tooltip("Duración de la animación Fase2.")]
    [SerializeField] private float duracionFase2 = 2.5f;
    [SerializeField] private float multiplicadorDanoFase2 = 1.25f;

    [Header("Ataque especial (dash, solo fase 2)")]
    [SerializeField] private float danoEspecial = 40f;
    [SerializeField] private float distanciaMinEspecial = 4f;
    [SerializeField] private float distanciaMaxEspecial = 14f;
    [Tooltip("Si el jugador está más lejos que esto, usa el especial aunque el plan fuera otro.")]
    [SerializeField] private float distanciaHuida = 8f;
    [Tooltip("Tiempo mirando al jugador antes de lanzarse (da chance de esquivar).")]
    [SerializeField] private float preparacionEspecial = 0.7f;
    [SerializeField] private float velocidadDash = 22f;
    [SerializeField] private float duracionDash = 0.6f;
    [SerializeField] private float radioDash = 1.5f;
    [Tooltip("Tiempo que queda expuesto después del dash.")]
    [SerializeField] private float recuperacionEspecial = 0.9f;
    [SerializeField] private float cooldownEspecial = 7f;
    [Tooltip("Probabilidad de que el próximo ataque planeado sea el especial.")]
    [SerializeField, Range(0f, 1f)] private float probabilidadEspecial = 0.6f;

    [Header("Muerte")]
    [SerializeField] private float despawnTrasMuerte = 6f;

    [Header("Animator (nombres de los parámetros)")]
    [SerializeField] private string parametroCaminar = "Caminar";           // Bool
    [SerializeField] private string parametroAtacar = "Atacar";             // Trigger
    [SerializeField] private string parametroEspecial = "AtaqueEspecial";   // Trigger
    [SerializeField] private string parametroFase2 = "Fase2";               // Trigger
    [SerializeField] private string parametroMorir = "Morir";               // Trigger

    private enum Estado
    {
        Perseguir,
        Atacando,
        Especial,
        TransicionFase2,
        Muerto
    }

    private NavMeshAgent agent;
    private Health salud;
    private Animator animator;

    private Estado estado = Estado.Perseguir;
    private bool enFase2;
    private bool muerto;
    private bool planEspecial;

    private Transform objetivo;
    private float proximoAtaque;
    private float proximoEspecial;
    private float proximaRuta;

    private int hashCaminar;
    private int hashAtacar;
    private int hashEspecial;
    private int hashFase2;
    private int hashMorir;
    private EnemyAudio audioBoss;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        salud = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();

        hashCaminar = Animator.StringToHash(parametroCaminar);
        hashAtacar = Animator.StringToHash(parametroAtacar);
        hashEspecial = Animator.StringToHash(parametroEspecial);
        hashFase2 = Animator.StringToHash(parametroFase2);
        hashMorir = Animator.StringToHash(parametroMorir);

        audioBoss = GetComponent<EnemyAudio>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Los clientes no mueven al boss: reciben la posición y las animaciones del servidor.
            if (agent != null)
                agent.enabled = false;

            return;
        }

        ValidarAnimator();

        agent.speed = velocidadFase1;
        salud.OnMuerto += AlMorir;
    }

    public override void OnNetworkDespawn()
    {
        if (salud != null)
            salud.OnMuerto -= AlMorir;
    }

    // ───────────────────────── VIDA ─────────────────────────

    private float FraccionVida()
    {
        float actual = salud.VidaActual;
        float maxima = salud.VidaMaxima;

        return maxima > 0f ? actual / maxima : 1f;
    }

    // ───────────────────────── BUCLE PRINCIPAL ─────────────────────────

    private void Update()
    {
        if (!IsServer || muerto)
            return;

        ActualizarAnimacionCaminar();

        if (estado != Estado.Perseguir)
            return;

        // Pasa a fase 2 cuando baja de la fracción configurada.
        if (!enFase2 && FraccionVida() <= fraccionVidaFase2)
        {
            StartCoroutine(TransicionFase2());
            return;
        }

        Health jugador = ObjetivosJugadores.MasCercano(transform.position);

        if (jugador == null)
        {
            objetivo = null;
            DetenerAgente();
            return;
        }

        objetivo = jugador.transform;
        float distancia = DistanciaHorizontal(transform.position, objetivo.position);

        Decidir(distancia);

        // Si Decidir arrancó un ataque, el estado cambió y no hay que perseguir.
        if (estado != Estado.Perseguir)
            return;

        Perseguir();
    }

    private void Perseguir()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.stoppingDistance = rangoAtaque * 0.7f;

        if (Time.time >= proximaRuta)
        {
            proximaRuta = Time.time + intervaloRuta;

            bool jugadorEnNavMesh = NavMesh.SamplePosition(
                objetivo.position,
                out NavMeshHit hit,
                5f,
                NavMesh.AllAreas
            );

            Debug.Log(
                $"[Boss] Agente activo: {agent.isActiveAndEnabled} | " +
                $"Jugador en NavMesh: {jugadorEnNavMesh} | " +
                $"Posición boss: {transform.position} | " +
                $"Posición jugador: {objetivo.position} | " +
                $"Destino aceptado: {agent.SetDestination(objetivo.position)}"
            );
        }

        MirarAlObjetivo();
    }

    // ───────────────────────── ELECCIÓN DE ATAQUE ─────────────────────────

    private void Decidir(float distancia)
    {
        bool normalListo = Time.time >= proximoAtaque;
        bool especialListo = enFase2 && Time.time >= proximoEspecial;

        bool franjaEspecial = distancia >= distanciaMinEspecial && distancia <= distanciaMaxEspecial;
        bool jugadorHuyendo = distancia > distanciaHuida;

        // 1) Dash para cerrar distancia: si era el plan, o si el jugador se aleja.
        if (especialListo && franjaEspecial && (planEspecial || jugadorHuyendo))
        {
            StartCoroutine(AtaqueEspecial());
            return;
        }

        // 2) Cuerpo a cuerpo: siempre el ataque normal.
        if (normalListo && distancia <= rangoAtaque)
        {
            StartCoroutine(AtaqueNormal());
        }
    }

    // Después de cada ataque se sortea qué va a querer usar la próxima vez.
    private void SortearPlan()
    {
        planEspecial = enFase2 && Random.value < probabilidadEspecial;
    }

    // ───────────────────────── FASE 2 ─────────────────────────

    private IEnumerator TransicionFase2()
    {
        estado = Estado.TransicionFase2;
        Debug.Log("[Boss] Fase 2");

        DetenerAgente();
        animator.SetTrigger(hashFase2);
        if (audioBoss != null) audioBoss.ReproducirFase2();

        yield return new WaitForSeconds(duracionFase2);

        if (muerto)
            yield break;

        enFase2 = true;
        agent.speed = velocidadFase2;
        SortearPlan();

        ReanudarAgente();
        estado = Estado.Perseguir;
    }

    // ───────────────────────── ATAQUE NORMAL ─────────────────────────

    private IEnumerator AtaqueNormal()
    {
        estado = Estado.Atacando;

        DetenerAgente();
        animator.SetTrigger(hashAtacar);
        if (audioBoss != null) audioBoss.ReproducirAtaque();

        bool golpeo = false;

        for (float t = 0f; t < duracionAtaque; t += Time.deltaTime)
        {
            if (muerto)
                yield break;

            // Sigue al jugador con la mirada hasta que el golpe conecta.
            if (!golpeo)
                MirarAlObjetivo();

            if (!golpeo && t >= retrasoGolpe)
            {
                golpeo = true;
                GolpeNormal();
            }

            yield return null;
        }

        proximoAtaque = Time.time + cooldownAtaque;
        SortearPlan();

        ReanudarAgente();
        estado = Estado.Perseguir;
    }

    private void GolpeNormal()
    {
        Vector3 centro = transform.position + transform.forward * (rangoAtaque * 0.5f) + Vector3.up;
        Collider[] impactos = Physics.OverlapSphere(centro, rangoAtaque, ~0, QueryTriggerInteraction.Ignore);

        HashSet<ulong> golpeados = new HashSet<ulong>();

        foreach (Collider c in impactos)
        {
            Health jugador = c.GetComponentInParent<Health>();

            if (jugador == null || !jugador.EsJugador || jugador.EstaMuerto)
                continue;

            Vector3 haciaJugador = jugador.transform.position - transform.position;
            haciaJugador.y = 0f;

            if (Vector3.Angle(transform.forward, haciaJugador) > anguloAtaque * 0.5f)
                continue;

            if (!golpeados.Add(jugador.NetworkObjectId))
                continue;

            jugador.TakeDamage(DanoActual(danoAtaque), ReglasCombate.AtacanteNeutral);
        }
    }

    // ───────────────────────── ATAQUE ESPECIAL (DASH) ─────────────────────────

    private IEnumerator AtaqueEspecial()
    {
        estado = Estado.Especial;
        Debug.Log("[Boss] Ataque especial");

        DetenerAgente();
        animator.SetTrigger(hashEspecial);
        if (audioBoss != null) audioBoss.ReproducirPreparacionEspecial();

        // 1) Preparación: mira al jugador, que tiene tiempo de verlo venir y esquivar.
        for (float t = 0f; t < preparacionEspecial; t += Time.deltaTime)
        {
            if (muerto)
                yield break;

            MirarAlObjetivo();
            yield return null;
        }

        // 2) Dash en línea recta hacia donde estaba el jugador al terminar la preparación.
        if (audioBoss != null) audioBoss.ReproducirDash();
        Vector3 direccion = transform.forward;
        direccion.y = 0f;
        direccion.Normalize();

        HashSet<ulong> golpeados = new HashSet<ulong>();

        for (float t = 0f; t < duracionDash; t += Time.deltaTime)
        {
            if (muerto)
                yield break;

            Vector3 paso = direccion * velocidadDash * Time.deltaTime;

            // Se corta si el paso se sale del NavMesh (pared o borde).
            Vector3 pie = transform.position - Vector3.up * agent.baseOffset;

            if (NavMesh.Raycast(pie, pie + paso, out NavMeshHit borde, NavMesh.AllAreas))
                break;

            agent.Move(paso);
            DanarJugadoresEnDash(golpeados);

            yield return null;
        }

        // 3) Recuperación: queda expuesto un momento.
        yield return new WaitForSeconds(recuperacionEspecial);

        if (muerto)
            yield break;

        proximoEspecial = Time.time + cooldownEspecial;
        proximoAtaque = Time.time + 0.5f;
        SortearPlan();

        ReanudarAgente();
        estado = Estado.Perseguir;
    }

    private void DanarJugadoresEnDash(HashSet<ulong> golpeados)
    {
        Collider[] impactos = Physics.OverlapSphere(
            transform.position + Vector3.up, radioDash, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider c in impactos)
        {
            Health jugador = c.GetComponentInParent<Health>();

            if (jugador == null || !jugador.EsJugador || jugador.EstaMuerto)
                continue;

            if (!golpeados.Add(jugador.NetworkObjectId))
                continue;

            jugador.TakeDamage(DanoActual(danoEspecial), ReglasCombate.AtacanteNeutral);
        }
    }

    // ───────────────────────── MUERTE ─────────────────────────

    private void AlMorir(ulong atacanteId)
    {
        muerto = true;
        estado = Estado.Muerto;

        StopAllCoroutines();

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        animator.SetTrigger(hashMorir);
        if (audioBoss != null) audioBoss.ReproducirMuerte();

        if (despawnTrasMuerte > 0f)
            StartCoroutine(DespawnTrasEspera());
    }

    private IEnumerator DespawnTrasEspera()
    {
        yield return new WaitForSeconds(despawnTrasMuerte);

        // Si otro script ya lo despawneó, no hace nada.
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }

    // ───────────────────────── AUXILIARES ─────────────────────────

    private float DanoActual(float baseDano)
    {
        return enFase2 ? baseDano * multiplicadorDanoFase2 : baseDano;
    }

    private void DetenerAgente()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void ReanudarAgente()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private void ActualizarAnimacionCaminar()
    {
        bool moviendo = estado == Estado.Perseguir &&
                        agent.enabled &&
                        agent.velocity.sqrMagnitude > 0.05f;

        animator.SetBool(hashCaminar, moviendo);
    }

    private void MirarAlObjetivo()
    {
        if (objetivo == null)
            return;

        Vector3 direccion = objetivo.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direccion),
            velocidadGiro * Time.deltaTime);
    }

    private float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Avisa una sola vez si el Animator no tiene alguno de los parámetros.
    private void ValidarAnimator()
    {
        if (animator == null)
        {
            Debug.LogError("[Boss] No se encontró el Animator del boss.");
            return;
        }

        List<string> faltan = new List<string>();

        foreach (string nombre in new[] { parametroCaminar, parametroAtacar, parametroEspecial, parametroFase2, parametroMorir })
        {
            bool existe = false;

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == nombre)
                    existe = true;
            }

            if (!existe)
                faltan.Add(nombre);
        }

        if (faltan.Count > 0)
            Debug.LogWarning("[Boss] Al Animator le faltan los parámetros: " + string.Join(", ", faltan));
    }
}