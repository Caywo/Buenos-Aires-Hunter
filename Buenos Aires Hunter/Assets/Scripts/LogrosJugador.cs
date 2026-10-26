using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// IDs de los logros. Tienen que coincidir EXACTAMENTE con el campo 'id' de cada asset LogroData.
/// </summary>
public static class IdsLogros
{
    // Combate
    public const string PrimeraBaja = "PRIMERA_BAJA";
    public const string Bajas100 = "BAJAS_100";
    public const string BajasPuno5 = "BAJAS_PUNO_5";
    public const string BajaEnElAire = "BAJA_EN_EL_AIRE";
    public const string Racha3 = "RACHA_3";

    // Extracción
    public const string PrimeraExtraccion = "EXTRACCION_PRIMERA";
    public const string ExtraccionAlLimite = "EXTRACCION_AL_LIMITE";
    public const string ExtraccionEnRojo = "EXTRACCION_EN_ROJO";

    // Supervivencia
    public const string SupervRonda5 = "SUPERV_RONDA_5";
    public const string SupervRonda15 = "SUPERV_RONDA_15";
    public const string SupervSinDano = "SUPERV_SIN_DANO";

    // Tienda
    public const string TiendaPrimera = "TIENDA_PRIMERA";
    public const string TiendaGasto1000 = "TIENDA_GASTO_1000";

    // Duelo
    public const string DueloPrimero = "DUELO_PRIMERO";
    public const string DueloRondaPerfecta = "DUELO_RONDA_PERFECTA";
    public const string DueloBarrida = "DUELO_BARRIDA";
}

/// <summary>Eventos que el SERVIDOR le avisa al dueño del jugador.</summary>
public enum EventoLogro : byte
{
    Baja = 0,               // valor: 1 si fue con puño, 0 si fue con arma
    Extraccion = 1,         // valor: vida restante en % (0-100), extra: saldo de la SUBE
    Compra = 2,             // valor: precio pagado
    RondaDueloGanada = 3,   // valor: 1 si la ronda fue perfecta (sin recibir daño)
    DueloGanado = 4         // valor: 1 si ganó sin perder ninguna ronda
}

/// <summary>
/// Va en el prefab del jugador. Funciona así:
///  - Lo que SOLO el servidor sabe (quién mató a quién, quién pagó el bondi...) llega por Notificar() y un ClientRpc al dueño.
///  - Lo que el dueño puede ver solo (rondas de supervivencia, daño recibido) se vigila acá, en su propia instancia.
///  - Los contadores y los logros otorgados los registra LogrosManager (PlayerPrefs de ESA computadora): cada jugador tiene los suyos.
///  - El desbloqueo se hace con LogrosManager.Instance.Desbloquear(id).
/// </summary>
public class LogrosJugador : NetworkBehaviour
{
    private CharacterController controller;
    private Health salud;

    // Supervivencia
    private EnemySpawnerSupervivencia spawner;
    private float proximaBusquedaSpawner;
    private int ultimaRonda;
    private bool danoEnRonda;
    private float vidaPrevia = -1f;

    // Racha de bajas
    private readonly Queue<float> tiemposBajas = new Queue<float>();

    private bool avisoSinManager;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        salud = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (salud != null) salud.OnVidaCambiada += AlCambiarVida;
    }

    public override void OnNetworkDespawn()
    {
        if (salud != null) salud.OnVidaCambiada -= AlCambiarVida;
        if (LogrosManager.Instance != null) LogrosManager.Instance.Guardar();
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        VigilarSupervivencia();
    }

    // ───────────────────────── Servidor → dueño ─────────────────────────

    /// <summary>SOLO SERVIDOR. Le avisa al dueño de este jugador que pasó algo relevante para los logros.</summary>
    public void Notificar(EventoLogro evento, int valor = 0, int extra = 0)
    {
        if (!IsServer) return;

        NotificarClientRpc((byte)evento, valor, extra, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    /// <summary>SOLO SERVIDOR. Todos los jugadores vivos que viajan en el bondi cuentan como extraídos.</summary>
    public static void NotificarExtraccionATodos()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var cliente in nm.ConnectedClientsList)
        {
            var obj = cliente.PlayerObject;
            if (obj == null) continue;

            var logros = obj.GetComponent<LogrosJugador>();
            var vida = obj.GetComponent<Health>();
            var inventario = obj.GetComponent<PlayerInventory>();
            if (logros == null || vida == null || inventario == null || vida.EstaMuerto) continue;

            int vidaPct = vida.VidaMaxima > 0f ? Mathf.RoundToInt(100f * vida.VidaActual / vida.VidaMaxima) : 0;
            logros.Notificar(EventoLogro.Extraccion, vidaPct, inventario.saldoSube.Value);
        }
    }

    [ClientRpc]
    private void NotificarClientRpc(byte evento, int valor, int extra, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        Procesar((EventoLogro)evento, valor, extra);
    }

    // ───────────────────────── Reglas ─────────────────────────

    private void Procesar(EventoLogro evento, int valor, int extra)
    {
        switch (evento)
        {
            case EventoLogro.Baja:
                ProcesarBaja(valor == 1);
                break;

            case EventoLogro.Extraccion:
                Otorgar(IdsLogros.PrimeraExtraccion);
                if (valor < 10) Otorgar(IdsLogros.ExtraccionAlLimite);
                if (extra < 0) Otorgar(IdsLogros.ExtraccionEnRojo);
                break;

            case EventoLogro.Compra:
                Otorgar(IdsLogros.TiendaPrimera);
                if (Sumar("gasto_tienda", valor) >= 1000) Otorgar(IdsLogros.TiendaGasto1000);
                break;

            case EventoLogro.RondaDueloGanada:
                if (valor == 1) Otorgar(IdsLogros.DueloRondaPerfecta);
                break;

            case EventoLogro.DueloGanado:
                Otorgar(IdsLogros.DueloPrimero);
                if (valor == 1) Otorgar(IdsLogros.DueloBarrida);
                break;
        }
    }

    private void ProcesarBaja(bool conPuno)
    {
        int total = Sumar("bajas", 1);
        Otorgar(IdsLogros.PrimeraBaja);
        if (total >= 100) Otorgar(IdsLogros.Bajas100);

        if (conPuno && Sumar("bajas_puno", 1) >= 5) Otorgar(IdsLogros.BajasPuno5);

        // Salto de fe: la baja llega con la demora de la red, así que es aproximado
        if (controller != null && !controller.isGrounded) Otorgar(IdsLogros.BajaEnElAire);

        // Lluvia de plomo: 3 bajas en 3 segundos o menos
        float ahora = Time.time;
        tiemposBajas.Enqueue(ahora);
        while (tiemposBajas.Count > 3) tiemposBajas.Dequeue();
        if (tiemposBajas.Count == 3 && ahora - tiemposBajas.Peek() <= 3f) Otorgar(IdsLogros.Racha3);
    }

    // ───────────────────────── Vigilancia local (supervivencia) ─────────────────────────

    private void AlCambiarVida(float actual, float maxima)
    {
        if (vidaPrevia >= 0f && actual < vidaPrevia) danoEnRonda = true;
        vidaPrevia = actual;
    }

    private void VigilarSupervivencia()
    {
        if (spawner == null)
        {
            // Solo existe en supervivencia: se busca una vez por segundo
            if (Time.time < proximaBusquedaSpawner) return;
            proximaBusquedaSpawner = Time.time + 1f;
            spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();
            return;
        }

        int ronda = spawner.rondaActual.Value;
        if (ronda == ultimaRonda) return;

        // Cambió la ronda: la anterior terminó. Si no se recibió daño en toda esa ronda, hay logro.
        bool habiaRondaAnterior = ultimaRonda > 0;
        bool vivo = salud == null || !salud.EstaMuerto;
        if (habiaRondaAnterior && !danoEnRonda && vivo) Otorgar(IdsLogros.SupervSinDano);

        danoEnRonda = false;
        ultimaRonda = ronda;

        if (ronda >= 5) Otorgar(IdsLogros.SupervRonda5);
        if (ronda >= 15) Otorgar(IdsLogros.SupervRonda15);
    }

    // ───────────────────────── Registro ─────────────────────────

    private int Sumar(string contador, int cantidad)
    {
        var manager = LogrosManager.Instance;
        return manager != null ? manager.Sumar(contador, cantidad) : 0;
    }

    private void Otorgar(string id)
    {
        var manager = LogrosManager.Instance;
        if (manager == null)
        {
            if (!avisoSinManager)
            {
                avisoSinManager = true;
                Debug.LogWarning("[Logros] No hay LogrosManager: los logros no se pueden registrar. " +
                                 "Tiene que existir en la escena MenuPrincipal, en un objeto raíz.");
            }
            return;
        }

        if (manager.EstaDesbloqueado(id)) return; // ya lo tenía

        Debug.Log($"[Logros] Desbloqueado: {id}");
        manager.Desbloquear(id);
    }
}
