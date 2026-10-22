using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Va en la raíz del prefab HUD.
// Solo PRESENTA datos: no calcula vida ni munición. El dev llama a estos métodos.
public class HUDController : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] Image vidaFill;
    [Tooltip("Opcional: barra clara detrás del Fill que baja lento (efecto de daño).")]
    [SerializeField] Image vidaDelay;
    [SerializeField] TMP_Text txtVida;
    [SerializeField] HitEffect hitEffect;
    [SerializeField] float retrasoDelay = 0.4f;
    [SerializeField] float velocidadDelay = 0.6f;   // fracción de barra por segundo

    [Header("Munición")]
    [SerializeField] GameObject grupoBalas;
    [SerializeField] TMP_Text txtBalas;
    [SerializeField] int cargadorBajo = 3;
    [SerializeField] Color colorNormal = Color.white;
    [SerializeField] Color colorBajo = new Color(1f, 0.25f, 0.2f);

    [Header("Dinero")]
    [SerializeField] TMP_Text txtDinero;
    [SerializeField] string prefijoDinero = "";      // ej: "$ " si el ícono no lo incluye

    [Header("Hotbar (en orden: Slot1, Slot2, ...)")]
    [SerializeField] SlotUI[] slots;

    [Header("Supervivencia (opcional)")]
    [SerializeField] GameObject grupoSupervivencia;
    [SerializeField] TMP_Text txtRonda;
    [SerializeField] TMP_Text txtContador;
    [SerializeField] string formatoRonda = "RONDA {0}";
    [SerializeField] string formatoContador = "{0}";

    // ---- estado interno ----
    float vidaObjetivo = 1f;
    float retrasoRestante;
    bool[] desbloqueado;
    int seleccionado = -1;

    void Awake()
    {
        int n = slots != null ? slots.Length : 0;
        desbloqueado = new bool[n];
        RefrescarSlots();

        if (vidaFill != null) vidaFill.fillAmount = 1f;
        if (vidaDelay != null) vidaDelay.fillAmount = 1f;
    }

    void Update()
    {
        ActualizarDelayVida();
    }

    // =============== VIDA ===============

    public void ActualizarVida(float actual, float maxima)
    {
        if (maxima <= 0f) return;

        float nueva = Mathf.Clamp01(actual / maxima);

        // Si perdió vida, la barra clara espera un momento antes de bajar
        if (nueva < vidaObjetivo) retrasoRestante = retrasoDelay;

        vidaObjetivo = nueva;

        if (vidaFill != null) vidaFill.fillAmount = nueva;

        // Si curó, la barra clara se alinea de inmediato
        if (vidaDelay != null && nueva > vidaDelay.fillAmount)
            vidaDelay.fillAmount = nueva;

        if (txtVida != null)
            txtVida.text = Mathf.CeilToInt(actual) + "/" + Mathf.CeilToInt(maxima);
    }

    void ActualizarDelayVida()
    {
        if (vidaDelay == null || vidaDelay.fillAmount <= vidaObjetivo) return;

        if (retrasoRestante > 0f)
        {
            retrasoRestante -= Time.unscaledDeltaTime;
            return;
        }

        vidaDelay.fillAmount = Mathf.MoveTowards(
            vidaDelay.fillAmount, vidaObjetivo, velocidadDelay * Time.unscaledDeltaTime);
    }

    // intensidad 0 a 1 (golpe fuerte = 1)
    public void MostrarGolpe(float intensidad = 1f)
    {
        if (hitEffect != null) hitEffect.Mostrar(intensidad);
    }

    // =============== MUNICIÓN ===============

    public void ActualizarMunicion(int cargador, int reserva)
    {
        if (txtBalas == null) return;

        txtBalas.text = cargador + " / " + reserva;
        txtBalas.color = cargador <= cargadorBajo ? colorBajo : colorNormal;
    }

    // Para armas sin munición (cuchillo, tarjeta, etc.)
    public void MostrarMunicion(bool visible)
    {
        if (grupoBalas != null) grupoBalas.SetActive(visible);
    }

    // =============== DINERO ===============

    public void ActualizarDinero(int cantidad)
    {
        if (txtDinero != null) txtDinero.text = prefijoDinero + cantidad;
    }

    // =============== HOTBAR ===============

    // Cuando el jugador consigue el objeto (el ícono pasa de gris a color)
    public void DesbloquearSlot(int indice)
    {
        if (!IndiceValido(indice)) return;
        desbloqueado[indice] = true;
        RefrescarSlots();
    }

    // Por si el jugador pierde el objeto (por ejemplo al morir)
    public void BloquearSlot(int indice)
    {
        if (!IndiceValido(indice)) return;
        desbloqueado[indice] = false;
        if (seleccionado == indice) seleccionado = -1;
        RefrescarSlots();
    }

    // Arma en mano. -1 = ninguna. Un slot bloqueado no se puede seleccionar.
    public void SeleccionarSlot(int indice)
    {
        if (indice == -1)
        {
            seleccionado = -1;
            RefrescarSlots();
            return;
        }

        if (!IndiceValido(indice) || !desbloqueado[indice]) return;

        seleccionado = indice;
        RefrescarSlots();
    }

    // Vuelve todo al estado inicial (nueva partida)
    public void ReiniciarSlots()
    {
        for (int i = 0; i < desbloqueado.Length; i++) desbloqueado[i] = false;
        seleccionado = -1;
        RefrescarSlots();
    }

    void RefrescarSlots()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            SlotUI.Estado estado = SlotUI.Estado.Bloqueado;
            if (desbloqueado[i])
                estado = (i == seleccionado) ? SlotUI.Estado.Seleccionado : SlotUI.Estado.Desbloqueado;

            slots[i].SetEstado(estado);
        }
    }

    bool IndiceValido(int i)
    {
        return slots != null && i >= 0 && i < slots.Length;
    }

    // =============== SUPERVIVENCIA ===============

    public void MostrarSupervivencia(bool visible)
    {
        if (grupoSupervivencia != null) grupoSupervivencia.SetActive(visible);
    }

    public void ActualizarRonda(int ronda)
    {
        if (txtRonda != null) txtRonda.text = string.Format(formatoRonda, ronda);
    }

    public void ActualizarContador(int valor)
    {
        if (txtContador != null) txtContador.text = string.Format(formatoContador, valor);
    }
}
