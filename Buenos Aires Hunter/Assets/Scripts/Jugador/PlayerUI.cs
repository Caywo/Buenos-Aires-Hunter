using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.Assertions.Must;

public class PlayerUI : MonoBehaviour
{
    [Header("HUD")]
    public GameObject hud;

    [Header("Existentes")]
    public TMP_Text textoSaldo;
    public GameObject fondoSaldo;
    public TMP_Text textoInteraccion;

    [Header("Combate")]
    public Image barraDeVida;
    public TMP_Text textoMunicion;
    public GameObject fondoMunicion;
    public TMP_Text textoRonda;
    public TMP_Text textoContador;
    public GameObject hitMarker;
    public float duracionHitMarker = 0.1f;
    public GameObject crosshair;

    [Header("Modo duelo")]
    [Tooltip("Elementos del HUD que NO aplican al duelo (ej: slots de la SUBE y del Wachin en la hotbar). " +
             "Solo se ocultan cuando empieza un duelo; en los demás modos no se tocan.")]
    public GameObject[] ocultarEnDuelo;

    private PlayerInventory inventarioLocal;
    private PlayerCombat combateLocal;
    private Health saludLocal;
    private float finHitMarker;
    private EnemySpawnerSupervivencia spawner;
    private bool avisoEmitido;
    private NetworkObject duenio; // jugador al que pertenece este HUD (el HUD vive dentro del prefab del jugador)
    private bool hudMostrado;
    private float proximaBusquedaSpawner;

    void Awake()
    {
        duenio = GetComponentInParent<NetworkObject>();
        if (hud != null) hud.SetActive(false);
    }
    void Update()
    {
        // El HUD vive dentro del prefab del jugador, así que cada jugador tiene una copia.
        // Solo la del jugador LOCAL se muestra y se actualiza; las de los jugadores remotos se apagan.
        if (duenio != null)
        {
            if (!duenio.IsSpawned) return;   // todavía no se creó en red
            if (!duenio.IsOwner)
            {
                enabled = false;
                return;
            }
        }

        if (hitMarker != null && hitMarker.activeSelf && Time.time >= finHitMarker)
            hitMarker.SetActive(false);

        BuscarJugadorLocal();

        // Cada bloque es independiente: si falta un componente, los demás siguen funcionando
        ActualizarSaldo();
        ActualizarVida();
        ActualizarMunicion();

        if (spawner == null)
        {
            // Solo existe en supervivencia: buscarlo una vez por segundo, no en cada frame
            if (Time.time >= proximaBusquedaSpawner)
            {
                spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();
                proximaBusquedaSpawner = Time.time + 1f;
            }

            if (textoRonda != null && !textoRonda.gameObject.activeSelf)
                textoRonda.gameObject.SetActive(true);
        }
        else
        {
            ActualizarRonda();
        }
    }

    // NUEVO: cada componente se busca por separado y se avisa UNA vez si falta alguno
    private void BuscarJugadorLocal()
    {
        if (inventarioLocal != null && saludLocal != null && combateLocal != null) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        NetworkObject playerObject = duenio;
        if (playerObject == null)
        {
            // HUD puesto en la escena (no dentro del jugador): buscar al jugador local
            if (NetworkManager.Singleton.LocalClient == null) return;
            playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        }
        if (playerObject == null) return;

        // Awake oculta el HUD: se muestra una vez que existe el jugador local
        if (!hudMostrado && hud != null)
        {
            hud.SetActive(true);
            hudMostrado = true;
        }

        if (inventarioLocal == null) inventarioLocal = playerObject.GetComponent<PlayerInventory>();
        if (saludLocal == null) saludLocal = playerObject.GetComponent<Health>();

        if (combateLocal == null)
        {
            combateLocal = playerObject.GetComponent<PlayerCombat>();
            if (combateLocal != null)
                combateLocal.OnImpactoConfirmado += MostrarHitMarker;
        }

        if (!avisoEmitido && (inventarioLocal == null || saludLocal == null || combateLocal == null))
        {
            avisoEmitido = true;
            Debug.LogWarning($"[PlayerUI] Al jugador local le falta algún componente. " +
                             $"PlayerInventory: {inventarioLocal != null}, Health: {saludLocal != null}, " +
                             $"PlayerCombat: {combateLocal != null}. Revisá el prefab del jugador.");
        }
    }

    private void ActualizarSaldo()
    {
        if (textoSaldo == null || inventarioLocal == null) return;

        bool mostrar = inventarioLocal.TieneSubeEquipada;
        textoSaldo.gameObject.SetActive(mostrar);
        if (fondoSaldo != null) fondoSaldo.SetActive(mostrar);
        if (mostrar)
            textoSaldo.text = $"$ {inventarioLocal.saldoSube.Value}";
    }

    // Vida
    private void ActualizarVida()
    {
        if (saludLocal == null) return;

        if (barraDeVida != null)
        {
            barraDeVida.fillAmount = saludLocal.VidaMaxima > 0f ? saludLocal.VidaActual / saludLocal.VidaMaxima : 0f;
        }
    }

    // Munición
    private void ActualizarMunicion()
    {
        if (textoMunicion == null || combateLocal == null) return;

        bool municionActiva = combateLocal.TryGetMunicionActiva(out int cargador, out int reserva);

        if (municionActiva)
            textoMunicion.text = combateLocal.Recargando ? "RECARGANDO..." : $"{cargador} / {reserva}";
        else
            textoMunicion.text = string.Empty;

        if (fondoMunicion != null) fondoMunicion.SetActive(municionActiva);
    }

    private void ActualizarRonda()
    {
        if (textoRonda == null) return;

        if (spawner == null) spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

        if (spawner != null)
        {
            textoRonda.gameObject.SetActive(true);
            if (spawner.tiempoRestante.Value > 0f)
            {
                if (textoContador != null)
                {
                    textoContador.gameObject.SetActive(true);
                    textoContador.text = "Próxima ronda en " + Mathf.CeilToInt(spawner.tiempoRestante.Value);
                }
            }
            else
            {
                if (textoContador != null) textoContador.gameObject.SetActive(false);
                textoRonda.text = "Ronda " + spawner.rondaActual.Value;
            }
        }
        else
        {
            textoRonda.gameObject.SetActive(false);
        }
    }
    private void MostrarHitMarker()
    {
        if (hitMarker == null) return;
        hitMarker.SetActive(true);
        finHitMarker = Time.time + duracionHitMarker;
    }

    private void OnDestroy()
    {
        if (combateLocal != null)
            combateLocal.OnImpactoConfirmado -= MostrarHitMarker;
    }

    public void ActualizarTexto(string mensajeInteraccion)
    {
        if (textoInteraccion != null)
            textoInteraccion.text = mensajeInteraccion;
    }

    /// <summary>Oculta los elementos que no aplican al duelo. Lo llama DuelManager en el jugador local.</summary>
    public void ConfigurarModoDuelo()
    {
        if (ocultarEnDuelo == null) return;

        foreach (GameObject elemento in ocultarEnDuelo)
        {
            if (elemento != null) elemento.SetActive(false);
        }
    }

    public void apagarCrosshair()
    {
        if (crosshair != null) crosshair.SetActive(false);
    }

    public void activarCrosshair()
    {
        if (crosshair != null) crosshair.SetActive(true);
    }
}


