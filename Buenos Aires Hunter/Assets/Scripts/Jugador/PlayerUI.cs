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

    private PlayerInventory inventarioLocal;
    private PlayerCombat combateLocal;
    private Health saludLocal;
    private float finHitMarker;
    private EnemySpawnerSupervivencia spawner;
    private bool avisoEmitido;

    void Awake()
    {
        if (hud != null) hud.SetActive(false);
    }
    void Update()
    {
        if (hitMarker != null && hitMarker.activeSelf && Time.time >= finHitMarker)
            hitMarker.SetActive(false);

        BuscarJugadorLocal();

        // Cada bloque es independiente: si falta un componente, los demás siguen funcionando
        ActualizarSaldo();
        ActualizarVida();
        ActualizarMunicion();

        if (spawner == null)
        {
            spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();
            textoRonda.gameObject.SetActive(true);
        }
        else {
            ActualizarRonda();
        }
    }

    // NUEVO: cada componente se busca por separado y se avisa UNA vez si falta alguno
    private void BuscarJugadorLocal()
    {
        if (inventarioLocal != null && saludLocal != null && combateLocal != null) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (NetworkManager.Singleton.LocalClient == null) return;

        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject == null) return;

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
        fondoSaldo.gameObject.SetActive(mostrar);
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
        {
            fondoMunicion.SetActive(municionActiva);
            textoMunicion.text = combateLocal.Recargando ? "RECARGANDO..." : $"{cargador} / {reserva}";

        }
        else
            textoMunicion.text = string.Empty;
            fondoMunicion.SetActive(municionActiva);
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
                textoContador.gameObject.SetActive(true);
                textoContador.text = "Próxima ronda en " + Mathf.CeilToInt(spawner.tiempoRestante.Value);
            }
            else
            {
                textoContador.gameObject.SetActive(false);
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

    public void apagarCrosshair()
    {
        crosshair.SetActive(false);
    }

    public void activarCrosshair()
    {
        crosshair.SetActive(true);
    }
}


