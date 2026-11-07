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
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    private void Start()
    {
        BuscarJugadorLocal();
        ActualizarCursor(false);
    }

    private void Update()
    {
        // Permitir probar con ESC o con la tecla P (ideal para probar en Editor)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (volviendoAlMenu) return;

            // 1. Si Opciones está activo, la tecla vuelve a la Pausa
            if (panelOpciones != null && panelOpciones.activeSelf)
            {
                VolverDeOpcionesAPausa();
            }
            // 2. Si la Pausa está abierta, se cierra
            else if (pausaAbierta)
            {
                CerrarMenuDePausa();
            }
            // 3. Si la Pausa está cerrada, se abre
            else
            {
                AbrirMenuDePausa();
            }
        }
    }

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
        if (pausaAbierta || volviendoAlMenu)
            return;

        if (jugador == null)
            BuscarJugadorLocal();

        pausaAbierta = true;

        if (panelPausa != null) panelPausa.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        BloquearControles(true);
        ActualizarCursor(true);
    }

    public void CerrarMenuDePausa()
    {
        if (!pausaAbierta || volviendoAlMenu)
            return;

        pausaAbierta = false;

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

        BloquearControles(false);
        ActualizarCursor(false);
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
}