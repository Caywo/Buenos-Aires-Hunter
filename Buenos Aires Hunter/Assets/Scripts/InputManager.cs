using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.MovimientoActions movimiento;
    private PlayerInput.InventarioActions inventarioActions;
    private PlayerMotor motor;
    private PlayerMirar mirar;
    private PlayerInventory inventario;
   
    void Awake()
    {
        playerInput = new PlayerInput();
        movimiento = playerInput.Movimiento;
        inventarioActions = playerInput.Inventario;
        motor = GetComponent<PlayerMotor>();
        mirar = GetComponent<PlayerMirar>();
        inventario= GetComponent<PlayerInventory>();

        if (motor == null) Debug.LogError("InputManager: no se encontró PlayerMotor.", this);
        if (mirar == null) Debug.LogError("InputManager: no se encontró PlayerMirar.", this);
        if (inventario == null) Debug.LogError("InputManager: no se encontró PlayerInventory.", this);

        movimiento.Saltar.performed += ctx => motor.Saltar();

        inventarioActions.Disparar.performed += ctx => inventario.Disparar();

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

    void FixedUpdate()
    {
        if (!IsOwner) return;
        motor.ProcessMove(movimiento.Movimiento.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        mirar.ProcessMirar(movimiento.Mirar.ReadValue<Vector2>());
    }
}