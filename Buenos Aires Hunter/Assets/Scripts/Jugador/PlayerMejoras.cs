using Unity.Netcode;
using UnityEngine;

public class PlayerMejoras : NetworkBehaviour
{
    public float multiplicadorVelocidad = 1.3f;

    public NetworkVariable<bool> tieneMejoraVelocidad = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerMotor motor;
    private float velocidadBase;

    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
    }

    public override void OnNetworkSpawn()
    {
        if (motor != null) velocidadBase = motor.speed;

        if (IsServer)
        {
            tieneMejoraVelocidad.Value = false;
        }

        tieneMejoraVelocidad.OnValueChanged += (viejo, nuevo) => AplicarMejoraVelocidad(nuevo);
        AplicarMejoraVelocidad(tieneMejoraVelocidad.Value);
    }

    private void AplicarMejoraVelocidad(bool activa)
    {
        if (motor == null) return;
        motor.speed = activa ? velocidadBase * multiplicadorVelocidad : velocidadBase;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IntentarComprarVelocidadRpc(int precio)
    {
        if (tieneMejoraVelocidad.Value) return;

        var inventario = GetComponent<PlayerInventory>();
        if (inventario == null) return;
        if (!inventario.PagarConSube(precio)) return;

        tieneMejoraVelocidad.Value = true;
    }
}