using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.MovimientoActions movimiento;
    private PlayerInput.InventarioActions inventarioActions;
    private PlayerMotor motor;
    private PlayerLook mirar;
    private PlayerInventory inventario;
    private PlayerInteract interactuar;
    private Health salud;

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
        inventarioActions.Interactuar.performed += ctx => { if (!salud.EstaMuerto) interactuar.Interactuar(); };

        if (motor == null) Debug.LogError("InputManager: no se encontró PlayerMotor.", this);
        if (mirar == null) Debug.LogError("InputManager: no se encontró PlayerMirar.", this);
        if (inventario == null) Debug.LogError("InputManager: no se encontró PlayerInventory.", this);
        if (salud == null) Debug.LogError("InputManager: no se encontró Health.", this);

        movimiento.Saltar.performed += ctx => { if (!salud.EstaMuerto) motor.Saltar(); };

        // El disparo ya NO usa 'performed': se lee con IsPressed() en Update para permitir fuego automático.
        inventarioActions.Recargar.performed += ctx => inventario.Recargar();

        inventarioActions.Apuntar.performed += ctx => inventario.SetApuntando(true);
        inventarioActions.Apuntar.canceled += ctx => inventario.SetApuntando(false);

        inventarioActions.Items.performed += ctx =>
        {
            string tecla = ctx.control.name; // "1", "2", "3"
            if (int.TryParse(tecla, out int numero))
            {
                inventario.Equipar(numero - 1); // tecla 1 -> índice 0
            }
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            movimiento.Enable();
            inventarioActions.Enable();
            Cursor.lockState = CursorLockMode.Locked;
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
        movimiento.Disable();
        inventarioActions.Disable();
    }

    void Update()
    {
        if (!IsOwner || salud.EstaMuerto) return;

        var disparar = inventarioActions.Disparar;
        if (disparar.IsPressed())
        {
            // 'pulsacionNueva' permite distinguir armas semiautomáticas de automáticas
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
}