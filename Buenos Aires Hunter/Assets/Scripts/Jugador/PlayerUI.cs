using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.Assertions.Must;

public class PlayerUI : MonoBehaviour
{
    [Header("Existentes")]
    public TMP_Text textoSaldo;
    public TMP_Text textoInteraccion;

    [Header("Combate (todos opcionales)")]
    public Slider barraVida;
    public TMP_Text textoVida;
    public TMP_Text textoMunicion;
    public TMP_Text textoRonda;
    public GameObject hitMarker;
    public float duracionHitMarker = 0.1f;

    private PlayerInventory inventarioLocal;
    private PlayerCombat combateLocal;
    private Health saludLocal;
    private float finHitMarker;
    private EnemySpawnerSupervivencia spawner;
    private bool avisoEmitido;

    void Update()
    {
        if (hitMarker != null && hitMarker.activeSelf && Time.time >= finHitMarker)
            hitMarker.SetActive(false);

        BuscarJugadorLocal();

        // Cada bloque es independiente: si falta un componente, los demás siguen funcionando
        ActualizarSaldo();
        ActualizarVida();
        ActualizarMunicion();
        ActualizarRonda();
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

    // Saldo SUBE (como antes)
    private void ActualizarSaldo()
    {
        if (textoSaldo == null || inventarioLocal == null) return;

        bool mostrar = inventarioLocal.TieneSubeEquipada;
        textoSaldo.gameObject.SetActive(mostrar);
        if (mostrar)
            textoSaldo.text = $"$ {inventarioLocal.saldoSube.Value}";
    }

    // Vida
    private void ActualizarVida()
    {
        if (saludLocal == null) return;

        if (barraVida != null)
            barraVida.value = saludLocal.VidaMaxima > 0f ? saludLocal.VidaActual / saludLocal.VidaMaxima : 0f;
        if (textoVida != null)
            textoVida.text = Mathf.CeilToInt(saludLocal.VidaActual).ToString();
    }

    // Munición
    private void ActualizarMunicion()
    {
        if (textoMunicion == null || combateLocal == null) return;

        if (combateLocal.TryGetMunicionActiva(out int cargador, out int reserva))
            textoMunicion.text = combateLocal.Recargando ? "RECARGANDO..." : $"{cargador} / {reserva}";
        else
            textoMunicion.text = string.Empty;
    }

    // Ronda (modo supervivencia): sin cambios respecto a tu versión
    private void ActualizarRonda()
    {
        if (textoRonda == null) return;

        if (spawner == null) spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

        if (spawner != null)
        {
            textoRonda.gameObject.SetActive(true);
            if (spawner.tiempoRestante.Value > 0f)
            {
                textoRonda.text = "Próxima ronda en " + Mathf.CeilToInt(spawner.tiempoRestante.Value);
            }
            else
            {
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
}
