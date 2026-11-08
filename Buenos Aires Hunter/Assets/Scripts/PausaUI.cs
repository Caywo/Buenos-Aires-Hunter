using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PausaUI : MonoBehaviour
{
    [Header("UI Paneles")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelOpciones; // <--- ASIGNAR EN INSPECTOR

    private PlayerInventory jugador;
    private PlayerLook mirar;
    private PlayerMotor motor;

    private bool pausaAbierta;
    private bool volviendoAlMenu;

    public bool PausaAbierta => pausaAbierta;


    private void Awake()
    {
        // Garantizamos que el juego SIEMPRE inicie corriendo y no en pausa
        Time.timeScale = 1f;
        pausaAbierta = false;
        volviendoAlMenu = false;
    }

    private void Start()
    {
        // Aseguramos que los páneles arranquen apagados
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

/*
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (volviendoAlMenu) return;

            // 1. Si Opciones está abierto, volver a Pausa
            if (panelOpciones != null && panelOpciones.activeSelf)
            {
                VolverDeOpcionesAPausa();
            }
            // 2. Si la Pausa está CERRADA, abrirla
            else if (!pausaAbierta)
            {
                AbrirMenuDePausa();
            }
            // 3. Si la Pausa está ABIERTA, cerrarla
            else
            {
                CerrarMenuDePausa();
            }
        }
    }
*/
    private void BuscarJugadorLocal()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsListening)
            return;

        NetworkObject jugadorLocal = nm.SpawnManager.GetLocalPlayerObject();

        if (jugadorLocal == null)
            return;

        jugador = jugadorLocal.GetComponent<PlayerInventory>();
        mirar = jugadorLocal.GetComponent<PlayerLook>();
        motor = jugadorLocal.GetComponent<PlayerMotor>();
    }

    public void AbrirMenuDePausa()
    {
        pausaAbierta = true;
        Time.timeScale = 0f;

        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
            Debug.Log($"[PAUSA] panelPausa activado: {panelPausa.name} ahora está {panelPausa.activeSelf}");
        }
        else
        {
            Debug.LogError("[PAUSA] ¡ERROR! La referencia 'panelPausa' es NULL en el Inspector.");
        }

        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarMenuDePausa()
    {
        pausaAbierta = false;
        Time.timeScale = 1f; // Reanuda la física y el movimiento del jugador

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ───────────────────────── NAVEGACIÓN A OPCIONES ─────────────────────────

    /// <summary>
    /// Conectar al OnClick() del botón OPCIONES dentro del Menú de Pausa.
    /// </summary>
    public void AbrirOpcionesDesdePausa()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    /// <summary>
    /// Conectar al OnClick() del botón VOLVER dentro del Menú de Opciones (cuando está en el juego).
    /// </summary>
    public void VolverDeOpcionesAPausa()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (panelPausa != null) panelPausa.SetActive(true);
    }

    // ───────────────────────── CONTROLADORES Y CURSOR ─────────────────────────

    private void BloquearControles(bool bloquear)
    {
        if (jugador != null) jugador.inputBloqueado = bloquear;
        if (mirar != null) mirar.inputBloqueado = bloquear;
        if (motor != null) motor.inputBloqueado = bloquear;
    }

    private void ActualizarCursor(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    public void VolverAlMenu()
    {
        if (volviendoAlMenu) return;
        volviendoAlMenu = true;

        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        var spawnerSupervivencia = FindAnyObjectByType<EnemySpawnerSupervivencia>();
        if (spawnerSupervivencia != null) spawnerSupervivencia.enabled = false;

        Debug.Log("[VolverAlMenu] Cargando escena: MenuPrincipal");
        SceneManager.LoadScene("MenuPrincipal", LoadSceneMode.Single);

        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null) nm.Shutdown();

        volviendoAlMenu = false;
    }

    public void OnTogglePausa()
    {
        if (volviendoAlMenu) return;

        // Prioridad 1: Si el menú de Opciones está abierto, ESC vuelve al menú de Pausa
        if (panelOpciones != null && panelOpciones.activeSelf)
        {
            VolverDeOpcionesAPausa();
        }
        // Prioridad 2: Si el menú de Pausa está abierto, lo cierra y reanuda el juego
        else if (pausaAbierta)
        {
            CerrarMenuDePausa();
        }
        // Prioridad 3: Si el juego está en marcha, abre el menú de Pausa
        else
        {
            AbrirMenuDePausa();
        }
    }

}