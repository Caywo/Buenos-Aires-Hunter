using System;
using Unity.Netcode;
using UnityEngine;

public enum Jugada
{
    Piedra = 0,
    Papel = 1,
    Tijera = 2
}

/// <summary>
/// Piedra, papel o tijera con Horacio, al mejor de 3, apostando saldo de la SUBE.
/// Va en el prefab del jugador. Servidor autoritativo.
/// </summary>
[RequireComponent(typeof(PlayerInventory))]
public class MinijuegoPiedraPapelTijera : NetworkBehaviour
{
    [Header("Reglas")]
    [SerializeField] private int victoriasNecesarias = 2;

    [Header("Apuesta (saldo de la SUBE)")]
    [SerializeField] private int apuestaMinima = 50;
    [SerializeField] private int apuestaMaxima = 500;
    [Tooltip("La apuesta sube y baja de a este valor.")]
    [SerializeField] private int pasoApuesta = 50;
    [Tooltip("Ganancia al ganar, como fracción de la apuesta. 1 = gana lo mismo que apostó. 0.9 = 10% menos (saca dinero de la economía).")]
    [SerializeField, Range(0.5f, 1f)] private float gananciaPorVictoria = 1f;

    [Header("Límite de partidas (cuida la economía)")]
    [Tooltip("Máximo de partidas por jugador en toda la partida del mapa.")]
    [SerializeField] private int maxPartidasTotal = 3;
    [Tooltip("Máximo de partidas por ronda de descanso (solo en Supervivencia).")]
    [SerializeField] private int maxPartidasPorDescanso = 1;

    private PlayerInventory inventario;
    private EnemySpawnerSupervivencia spawner;
    private bool pausaPrevia;
    private float proximaBusqueda;

    // Solo servidor
    private bool partidaActiva;
    private int apuestaActual;
    private int puntosJugador;
    private int puntosVendedor;
    private int partidasUsadasTotal;
    private int partidasUsadasDescanso;

    // Sincronizadas, para que la UI sepa qué mostrar
    private readonly NetworkVariable<int> partidasDisponibles = new NetworkVariable<int>(0);
    private readonly NetworkVariable<bool> hayPartidaEnCurso = new NetworkVariable<bool>(false);

    // Eventos para la UI (se disparan en el dueño).
    public event Action<int, int, int> OnInicio;                       // puntosJugador, puntosVendedor, apuesta
    public event Action<int, int, int, int, int, bool, int> OnRonda;   // jugadaJugador, jugadaVendedor, resultado (1 gana, 0 empate, -1 pierde), puntosJugador, puntosVendedor, terminada, cambioSaldo
    public event Action<string> OnError;

    /// <summary>SOLO SERVIDOR. Se dispara al terminar una partida (true = ganó el jugador).</summary>
    public event Action<bool> OnPartidaTerminadaServidor;

    public int VictoriasNecesarias => victoriasNecesarias;
    public int ApuestaMinima => apuestaMinima;
    public int ApuestaMaxima => apuestaMaxima;
    public int PasoApuesta => Mathf.Max(1, pasoApuesta);
    public int PartidasDisponibles => partidasDisponibles.Value;
    public bool HayPartidaEnCurso => hayPartidaEnCurso.Value;

    private void Awake()
    {
        inventario = GetComponent<PlayerInventory>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            RecalcularDisponibles();
    }

    // ───────────────────────── LÍMITE DE PARTIDAS (servidor) ─────────────────────────

    private void Update()
    {
        if (!IsServer)
            return;

        if (spawner == null && Time.time >= proximaBusqueda)
        {
            proximaBusqueda = Time.time + 1f;
            spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

            if (spawner != null)
            {
                pausaPrevia = spawner.rondaEnPausa.Value;
                RecalcularDisponibles();
            }
        }

        if (spawner != null)
        {
            // Cada vez que empieza o termina un descanso, se renueva el cupo del descanso.
            bool pausa = spawner.rondaEnPausa.Value;

            if (pausa != pausaPrevia)
            {
                pausaPrevia = pausa;
                partidasUsadasDescanso = 0;
                RecalcularDisponibles();
            }
        }
    }

    private void RecalcularDisponibles()
    {
        int total = Mathf.Max(0, maxPartidasTotal - partidasUsadasTotal);

        // Sin spawner de rondas (por ejemplo en Extracción) solo rige el tope total.
        int descanso = spawner != null
            ? Mathf.Max(0, maxPartidasPorDescanso - partidasUsadasDescanso)
            : int.MaxValue;

        partidasDisponibles.Value = Mathf.Min(total, descanso);
    }

    // ───────────────────────── API LOCAL (la usa la UI) ─────────────────────────

    /// <summary>Con una partida en curso la retoma; si no, empieza una nueva con esa apuesta.</summary>
    public void Iniciar(int apuesta)
    {
        if (IsOwner)
            IniciarServerRpc(apuesta);
    }

    public void Jugar(Jugada jugada)
    {
        if (IsOwner)
            JugarServerRpc((int)jugada);
    }

    // ───────────────────────── SERVIDOR ─────────────────────────

    [ServerRpc]
    private void IniciarServerRpc(int apuesta)
    {
        // Si ya había una partida abierta (cerró la ventana), se retoma sin cobrar de nuevo.
        if (partidaActiva)
        {
            InicioClientRpc(puntosJugador, puntosVendedor, apuestaActual, AlDueno());
            return;
        }

        if (partidasDisponibles.Value <= 0)
        {
            ErrorClientRpc("No te quedan partidas por ahora.", AlDueno());
            return;
        }

        int paso = PasoApuesta;

        if (apuesta < apuestaMinima || apuesta > apuestaMaxima || (apuesta - apuestaMinima) % paso != 0)
        {
            ErrorClientRpc("Apuesta inválida.", AlDueno());
            return;
        }

        // No se puede apostar endeudándose.
        if (inventario.saldoSube.Value < apuesta)
        {
            ErrorClientRpc("No te alcanza el saldo de la SUBE.", AlDueno());
            return;
        }

        // Se descuenta la apuesta al empezar (si no, cerrar la ventana la devolvería).
        if (!inventario.PagarConSube(apuesta))
        {
            ErrorClientRpc("Necesitás la SUBE en mano.", AlDueno());
            return;
        }

        partidaActiva = true;
        hayPartidaEnCurso.Value = true;
        apuestaActual = apuesta;
        puntosJugador = 0;
        puntosVendedor = 0;

        // La partida consume el cupo apenas se apuesta.
        partidasUsadasTotal++;
        partidasUsadasDescanso++;
        RecalcularDisponibles();

        InicioClientRpc(0, 0, apuesta, AlDueno());
    }

    [ServerRpc]
    private void JugarServerRpc(int jugada)
    {
        if (jugada < 0 || jugada > 2 || !partidaActiva)
            return;

        // Horacio elige DESPUÉS de recibir la jugada del jugador.
        int vendedor = UnityEngine.Random.Range(0, 3);

        // 1 = gana el jugador, 0 = empate, -1 = gana Horacio
        int diferencia = (jugada - vendedor + 3) % 3;
        int resultado = diferencia == 0 ? 0 : diferencia == 1 ? 1 : -1;

        if (resultado == 1)
            puntosJugador++;
        else if (resultado == -1)
            puntosVendedor++;

        bool terminada = puntosJugador >= victoriasNecesarias || puntosVendedor >= victoriasNecesarias;

        int pj = puntosJugador;
        int pv = puntosVendedor;
        int cambioSaldo = 0;

        if (terminada)
        {
            bool gano = pj > pv;

            if (gano)
            {
                int ganancia = Mathf.RoundToInt(apuestaActual * gananciaPorVictoria);

                // Devuelve la apuesta y suma la ganancia.
                inventario.AgregarSaldo(apuestaActual + ganancia);
                cambioSaldo = ganancia;
            }
            else
            {
                // La apuesta ya se descontó al empezar.
                cambioSaldo = -apuestaActual;
            }

            partidaActiva = false;
            hayPartidaEnCurso.Value = false;
            apuestaActual = 0;
            puntosJugador = 0;
            puntosVendedor = 0;

            OnPartidaTerminadaServidor?.Invoke(gano);
        }

        RondaClientRpc(jugada, vendedor, resultado, pj, pv, terminada, cambioSaldo, AlDueno());
    }

    private ClientRpcParams AlDueno()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
    }

    // ───────────────────────── CLIENTE ─────────────────────────

    [ClientRpc]
    private void InicioClientRpc(int pj, int pv, int apuesta, ClientRpcParams rpcParams = default)
    {
        OnInicio?.Invoke(pj, pv, apuesta);
    }

    [ClientRpc]
    private void RondaClientRpc(int jugadaJugador, int jugadaVendedor, int resultado, int pj, int pv,
                                bool terminada, int cambioSaldo, ClientRpcParams rpcParams = default)
    {
        OnRonda?.Invoke(jugadaJugador, jugadaVendedor, resultado, pj, pv, terminada, cambioSaldo);
    }

    [ClientRpc]
    private void ErrorClientRpc(string mensaje, ClientRpcParams rpcParams = default)
    {
        OnError?.Invoke(mensaje);
    }
}