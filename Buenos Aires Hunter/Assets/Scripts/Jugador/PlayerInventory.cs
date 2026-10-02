using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [System.Serializable]
    public class ItemSlot
    {
        public ItemData item;
    }

    public ItemSlot[] items;
    public Transform weaponHolder;

    private NetworkVariable<int> indiceActivo = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<int>[] cantidades;
    private GameObject instanciaActual;
    private WeaponShoot weaponActual;
    private PlayerLook mirar;
    private PlayerCombat combate;
    private Health salud;
    private Camera camaraJugador;
    public int indiceSube = -1;
    public NetworkVariable<int> saldoSube = new NetworkVariable<int>(-2000,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    public bool TieneSubeEquipada => indiceActivo.Value == indiceSube;

    // NUEVO: acceso de solo lectura para PlayerCombat / HUD
    public int IndiceActivo => indiceActivo.Value;
    public WeaponShoot ArmaActual => weaponActual;

    public event System.Action<WeaponShoot> OnArmaCambiada;

    void Awake()
    {
        mirar = GetComponent<PlayerLook>();
        combate = GetComponent<PlayerCombat>();
        salud = GetComponent<Health>();
        camaraJugador = GetComponentInChildren<Camera>(true);
    }

    public override void OnNetworkSpawn()
    {
        cantidades = new NetworkVariable<int>[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            int inicial = items[i].item.esConsumible ? items[i].item.cantidadInicial : -1;
            cantidades[i] = new NetworkVariable<int>(inicial,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        }

        indiceActivo.OnValueChanged += (viejo, nuevo) => EquiparVisual(nuevo);
        EquiparVisual(indiceActivo.Value);
    }

    public void Equipar(int indice)
    {
        if (!IsOwner) return;
        if (salud != null && salud.EstaMuerto) return;

        // Si tocás la misma tecla del arma que ya tenés equipada, la guarda
        if (indice == indiceActivo.Value)
        {
            indiceActivo.Value = -1;
            return;
        }

        if (indice < 0 || indice >= items.Length) return;
        if (cantidades[indice].Value == 0) return;

        indiceActivo.Value = indice;
    }

    [ServerRpc]
    private void UsarServerRpc(int indice)
    {
        var data = items[indice].item;
        if (!data.esConsumible) return;
        if (cantidades[indice].Value <= 0) return;

        cantidades[indice].Value--;
        // Acá aplicás el efecto real (curar, tirar la granada, etc.)
    }

    private void EquiparVisual(int indice)
    {
        if (instanciaActual != null) Destroy(instanciaActual);
        instanciaActual = null;
        weaponActual = null;

        if (indice >= 0 && indice < items.Length)
        {
            var data = items[indice].item;
            if (data != null && data.prefabEnMano != null)
            {
                instanciaActual = Instantiate(data.prefabEnMano, weaponHolder);
                weaponActual = instanciaActual.GetComponent<WeaponShoot>();

                if (weaponActual != null)
                    weaponActual.Init(weaponHolder, mirar, camaraJugador, IsOwner);
            }
        }

        // Ahora sí se dispara el evento (antes se declaraba pero nunca se invocaba)
        OnArmaCambiada?.Invoke(weaponActual);
    }

    // El combate real vive en PlayerCombat (validado por el servidor)
    public void Disparar(bool pulsacionNueva)
    {
        if (!IsOwner || combate == null) return;
        combate.IntentarDisparar(pulsacionNueva);
    }

    public void Recargar()
    {
        if (!IsOwner || combate == null) return;
        combate.IntentarRecargar();
    }

    public int GetCantidad(int indice) => cantidades[indice].Value;

    public void SetApuntando(bool apuntando)
    {
        if (!IsOwner) return;
        if (weaponActual != null) weaponActual.SetAiming(apuntando);
    }

    public void AgregarSaldo(int cantidad)
    {
        if (!IsServer) return;
        saldoSube.Value += cantidad;
    }

    public bool PagarConSube(int monto)
    {
        if (!IsServer) return false;
        if (indiceActivo.Value != indiceSube) return false; // no tiene la SUBE en mano
        if (saldoSube.Value < -1200) return false; // saldo negativo

        saldoSube.Value -= monto;
        return true;
    }

}