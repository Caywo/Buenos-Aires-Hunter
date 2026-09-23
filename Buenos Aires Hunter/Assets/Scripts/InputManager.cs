using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private InputPersonaje inputPersonaje;
    private InputPersonaje.MovimientoActions movimiento;

    void Awake()
    {
        inputPersonaje = new InputPersonaje();
        movimiento = inputPersonaje.Movimiento;
    }

    private void OnEnable()
    {
        movimiento.Enable();
    }

    private void OnDisable()
    {
        movimiento.Disable();
    }
}