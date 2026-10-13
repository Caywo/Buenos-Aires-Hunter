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

    void Update()
    {
        if (hitMarker != null && hitMarker.activeSelf && Time.time >= finHitMarker)
            hitMarker.SetActive(false);

        if (inventarioLocal == null)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            if (NetworkManager.Singleton.LocalClient == null) return;

            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObject == null) return;

            inventarioLocal = playerObject.GetComponent<PlayerInventory>();
            saludLocal = playerObject.GetComponent<Health>();
            combateLocal = playerObject.GetComponent<PlayerCombat>();

            if (combateLocal != null)
                combateLocal.OnImpactoConfirmado += MostrarHitMarker;

            return;
        }

        // Saldo SUBE (como antes)
        bool mostrar = inventarioLocal.TieneSubeEquipada;
        textoSaldo.gameObject.SetActive(mostrar);
        if (mostrar)
            textoSaldo.text = $"$ {inventarioLocal.saldoSube.Value}";

        // Vida
        if (saludLocal != null)
        {
            if (barraVida != null)
                barraVida.value = saludLocal.VidaMaxima > 0f ? saludLocal.VidaActual / saludLocal.VidaMaxima : 0f;
            if (textoVida != null)
                textoVida.text = Mathf.CeilToInt(saludLocal.VidaActual).ToString();
        }

        // Munición
        if (textoMunicion != null && combateLocal != null)
        {
            if (combateLocal.TryGetMunicionActiva(out int cargador, out int reserva))
                textoMunicion.text = combateLocal.Recargando ? "RECARGANDO..." : $"{cargador} / {reserva}";
            else
                textoMunicion.text = string.Empty;
        }

        // Ronda (modo supervivencia)
        if (textoRonda != null)
        {
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
        textoInteraccion.text = mensajeInteraccion;
    }
}