using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.MovimientoActions movimiento;
    private PlayerMotor motor;
    private PlayerMirar mirar;

    void Awake()
    {
        playerInput = new PlayerInput();
        movimiento = playerInput.Movimiento;
        motor = GetComponent<PlayerMotor>();
        mirar = GetComponent<PlayerMirar>();
        movimiento.Saltar.performed += ctx => motor.Saltar();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            movimiento.Enable();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // Apaga la cámara y el audio de los jugadores remotos
            mirar.cam.enabled = false;
            var listener = mirar.cam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        movimiento.Disable();
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