using Unity.Netcode;
using UnityEngine;

public class PlayerDownState : NetworkBehaviour
{
    public int costoRevivir = 500;

    private Health vida;
    private PlayerInventory inventario;
    private PlayerMotor motor;
    private PlayerLook mirar;

    void Awake()
    {
        vida = GetComponent<Health>();
        inventario = GetComponent<PlayerInventory>();
        motor = GetComponent<PlayerMotor>();
        mirar = GetComponent<PlayerLook>();
    }

    public override void OnNetworkSpawn()
    {
        vida.OnMuerteCambiada += AlCambiarMuerte;
    }

    public override void OnNetworkDespawn()
    {
        vida.OnMuerteCambiada -= AlCambiarMuerte;
    }

    private void AlCambiarMuerte(bool muerto)
    {
        if (motor != null) motor.inputBloqueado = muerto;
        if (mirar != null) mirar.inputBloqueado = muerto;
        if (inventario != null) inventario.inputBloqueado = muerto;
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void IntentarRevivirRpc(ulong clientIdQuePaga)
    {
        if (!vida.EstaMuerto) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientIdQuePaga, out var cliente)) return;
        if (cliente.PlayerObject == null) return;

        var inventarioQuePaga = cliente.PlayerObject.GetComponent<PlayerInventory>();
        if (inventarioQuePaga == null) return;

        if (!inventarioQuePaga.PagarConSube(costoRevivir)) return;

        vida.Revivir();
    }
}