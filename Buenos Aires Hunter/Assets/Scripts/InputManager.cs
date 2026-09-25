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
        weapon = GetComponentInChildren<WeaponShoot>(true);

        if (motor == null) Debug.LogError("InputManager: no se encontró PlayerMotor.", this);
        if (mirar == null) Debug.LogError("InputManager: no se encontró PlayerMirar.", this);
        if (weapon == null) Debug.LogError("InputManager: no se encontró WeaponShoot en los hijos.", this);

        movimiento.Saltar.performed += ctx => motor.Saltar();
        movimiento.Disparar.performed += ctx =>
        {
            if (weapon != null) weapon.Disparar();
            else Debug.LogWarning("Se intentó disparar pero 'weapon' es null.");
        };
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