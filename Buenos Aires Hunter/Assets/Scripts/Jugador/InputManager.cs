using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.MovimientoActions movimiento;
    private PlayerInput.InventarioActions inventarioActions;
    private PlayerMotor motor;
    private PlayerLook mirar;
    private PlayerInventory inventario;
    private PlayerInteract interactuar;
    private Health salud; // NUEVO (combate)
    private TiendaUI tiendaUI;
    public PlayerUI playerUI;
    private PausaUI pausaUI;

    void Awake()
    {
        playerInput = new PlayerInput();
        movimiento = playerInput.Movimiento;
        inventarioActions = playerInput.Inventario;
        motor = GetComponent<PlayerMotor>();
        mirar = GetComponent<PlayerLook>();
        inventario = GetComponent<PlayerInventory>();
        interactuar = GetComponent<PlayerInteract>();
        salud = GetComponent<Health>();

        if (motor == null) Debug.LogError("InputManager: no se encontró PlayerMotor.", this);
        if (mirar == null) Debug.LogError("InputManager: no se encontró PlayerMirar.", this);
        if (inventario == null) Debug.LogError("InputManager: no se encontró PlayerInventory.", this);
        if (salud == null) Debug.LogError("InputManager: no se encontró Health.", this);

        // INTERACTUAR
        inventarioActions.Interactuar.performed += ctx =>
        {
            Debug.Log($"[InputManager] Tecla E detectada. IsOwner={IsOwner}, EstaMuerto={salud.EstaMuerto}, EstaEnPausa={EstaEnPausa()}");
            if (!salud.EstaMuerto && !EstaEnPausa())
                interactuar.Interactuar();
        };

        inventarioActions.Interactuar.started += ctx =>
        {
            if (!salud.EstaMuerto && !EstaEnPausa())
                interactuar.IniciarInteraccion();
        };

        inventarioActions.Interactuar.canceled += ctx =>
        {
            interactuar.TerminarInteraccion();
        };

        // SALTO
        movimiento.Saltar.performed += ctx =>
        {
            if (!salud.EstaMuerto && !EstaEnPausa())
                motor.Saltar();
        };

        // APUNTADO Y RECARGA
        inventarioActions.Recargar.performed += ctx =>
        {
            if (!EstaEnPausa())
                inventario.Recargar();
        };

        inventarioActions.Apuntar.performed += ctx =>
        {
            if (EstaEnPausa()) return;
            inventario.SetApuntando(true);
            playerUI.apagarCrosshair();
        };

        inventarioActions.Apuntar.canceled += ctx =>
        {
            inventario.SetApuntando(false);
            if (!EstaEnPausa())
                playerUI.activarCrosshair();
        };

        // INVENTARIO
        inventarioActions.Items.performed += ctx =>
        {
            if (EstaEnPausa()) return;

            string tecla = ctx.control.name; // "1", "2", "3"
            if (int.TryParse(tecla, out int numero))
            {
                inventario.Equipar(numero - 1); // tecla 1 -> índice 0
            }
        };

        inventarioActions.CerrarUI.performed += ctx =>
        {
            if (!IsOwner) return;

            if (tiendaUI == null)
                tiendaUI = GetComponentInChildren<TiendaUI>(true);

            if (pausaUI == null)
                pausaUI = GetComponentInChildren<PausaUI>(true);

            // Prioridad 1: cerrar la tienda si está abierta.
            if (tiendaUI != null && tiendaUI.EstaAbierta)
            {
                tiendaUI.CerrarTienda();
                return;
            }

            // Prioridad 2: gestionar la pausa y submenús.
            if (pausaUI != null)
            {
                pausaUI.OnTogglePausa();
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            movimiento.Enable();
            inventarioActions.Enable();
            SceneManager.sceneLoaded += AlCargarEscena;
            ActualizarEstadoCursor(SceneManager.GetActiveScene().name);
        }
        else
        {
            mirar.cam.enabled = false;
            var listener = mirar.cam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        movimiento.Disable();
        inventarioActions.Disable();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (!IsOwner) return;
        ActualizarEstadoCursor(escena.name);
    }

    private void ActualizarEstadoCursor(string nombreEscena)
    {
        if (nombreEscena == "MenuPrincipal")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // NUEVO (combate): fuego automático. 'pulsacionNueva' distingue armas semiautomáticas.
    // Si la tienda está abierta, PlayerInventory.Disparar() lo ignora por inputBloqueado.
    void Update()
    {
        if (!IsOwner || salud.EstaMuerto) return;

        var disparar = inventarioActions.Disparar;
        if (disparar.IsPressed())
        {
            inventario.Disparar(disparar.WasPressedThisFrame());
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || salud.EstaMuerto) return;
        motor.ProcessMove(movimiento.Movimiento.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        if (!IsOwner || salud.EstaMuerto) return;
        mirar.ProcessMirar(movimiento.Mirar.ReadValue<Vector2>());
    }

    private bool EstaEnPausa()
    {
        if (pausaUI == null)
            pausaUI = GetComponentInChildren<PausaUI>(true);

        return pausaUI != null && pausaUI.PausaAbierta;
    }

}