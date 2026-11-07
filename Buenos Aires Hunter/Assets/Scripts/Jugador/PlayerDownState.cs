
using Unity.Netcode;
using UnityEngine;

public class PlayerDownState : NetworkBehaviour
{
    [Header("Reanimación")]
    public int costoRevivir = 500;
    [SerializeField] private float tiempoParaRevivir = 3f;

    private Health vida;
    private PlayerInventory inventario;
    private PlayerMotor motor;
    private PlayerLook mirar;

    private float tiempoMantenido;
    private bool intentandoRevivir;
    public float TiempoRestante => Mathf.Max(0f, tiempoParaRevivir - tiempoMantenido);
    public bool IntentandoRevivir => intentandoRevivir;
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

    public void IniciarReanimacion()
    {
        if (!IsClient || !vida.EstaMuerto || intentandoRevivir)
            return;

        intentandoRevivir = true;
        tiempoMantenido = 0f;
    }

    public void CancelarReanimacion()
    {
        intentandoRevivir = false;
        tiempoMantenido = 0f;
    }

    void Update()
    {
        if (!IsClient || !IsSpawned || !intentandoRevivir)
            return;

        if (!vida.EstaMuerto)
        {
            CancelarReanimacion();
            return;
        }

        tiempoMantenido += Time.deltaTime;

        if (tiempoMantenido >= tiempoParaRevivir)
        {
            intentandoRevivir = false;
            IntentarRevivirRpc(NetworkManager.LocalClientId);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void IntentarRevivirRpc(ulong clientIdQuePaga)
    {
        if (!vida.EstaMuerto) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientIdQuePaga, out var cliente))
            return;

        if (cliente.PlayerObject == null) return;

        var inventarioQuePaga = cliente.PlayerObject.GetComponent<PlayerInventory>();
        if (inventarioQuePaga == null) return;

        if (!inventarioQuePaga.PagarConSube(costoRevivir)) return;

        vida.Revivir();
    }
}
