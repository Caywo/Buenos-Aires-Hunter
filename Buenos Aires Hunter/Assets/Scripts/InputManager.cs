using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class InputManager : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.MovimientoActions movimiento;
    private PlayerMotor motor;
    private PlayerMirar mirar;
    private WeaponShoot weapon;

    void Awake()
    {
        playerInput = new PlayerInput();
        movimiento = playerInput.Movimiento;
        motor = GetComponent<PlayerMotor>();
        mirar = GetComponent<PlayerMirar>();
        weapon = GetComponentInChildren<WeaponShoot>();

        movimiento.Saltar.performed += ctx => motor.Saltar();
        movimiento.Disparar.performed += ctx => weapon.Disparar();
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