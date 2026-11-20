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

        if (jugador == null || mirar == null || motor == null)
        {
            BuscarJugadorLocal();
        }

        // Bloquea movimiento, cámara y disparo localmente (funciona tanto con timeScale 0 como 1)
        BloquearControles(true);

        // Solo congelar el tiempo en Singleplayer
        if (!EsMultijugador())
        {
            Time.timeScale = 0f;
        }

        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
        }

        if (panelOpciones != null)
        {
            panelOpciones.SetActive(false);
        }

        ActualizarCursor(true);
    }

    public void CerrarMenuDePausa()
    {
        pausaAbierta = false;

        // Restauramos timeScale siempre por seguridad (por si un compañero se desconectó mientras estabas en pausa)
        Time.timeScale = 1f;

        if (jugador == null || mirar == null || motor == null)
        {
            BuscarJugadorLocal();
        }

        BloquearControles(false);

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);

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

        Time.timeScale = 1f;
        pausaAbierta = false;

        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        var spawnerSupervivencia = FindAnyObjectByType<EnemySpawnerSupervivencia>();
        if (spawnerSupervivencia != null) spawnerSupervivencia.enabled = false;

        if (NetworkManagerPersistente.Instance != null)
        {
            NetworkManagerPersistente.Instance.VolverAlMenu("MenuPrincipal");
        }
        else
        {
            SceneManager.LoadScene("MenuPrincipal", LoadSceneMode.Single);
            NetworkManager nm = NetworkManager.Singleton;
            if (nm != null) nm.Shutdown();
        }

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

    private bool EsMultijugador()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return false;

        // Si es cliente puro (invitado), siempre es multijugador.
        // Si es host, es multijugador si hay más de 1 cliente conectado.
        return !nm.IsHost || nm.ConnectedClientsList.Count > 1;
    }

}