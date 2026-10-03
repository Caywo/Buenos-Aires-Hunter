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

    private NetworkVariable<int> indiceActivo = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private NetworkVariable<int>[] cantidades;
    private GameObject instanciaActual;
    private WeaponShoot weaponActual;
    private PlayerLook mirar;
    private Camera camaraJugador;
    public int indiceSube = -1;
    public NetworkVariable<int> saldoSube = new NetworkVariable<int>(-1200,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    public bool TieneSubeEquipada => indiceActivo.Value == indiceSube;

    public event System.Action<WeaponShoot> OnArmaCambiada;
    private MeleeAttack melee;
    public bool inputBloqueado = false;
    void Awake()
    {
        mirar = GetComponent<PlayerLook>();
        camaraJugador = GetComponentInChildren<Camera>(true);
        melee = GetComponent<MeleeAttack>();
        melee.Init(camaraJugador);
    }
    public override void OnNetworkSpawn()
    {
        cantidades = new NetworkVariable<int>[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == null)
            {
                cantidades[i] = new NetworkVariable<int>(-1,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server);
                continue;
            }

            int inicial;
            if (!items[i].item.arrancaDesbloqueado)
            {
                inicial = 0; // bloqueado hasta comprarlo
            }
            else
            {
                inicial = items[i].item.esConsumible ? items[i].item.cantidadInicial : -1;
            }

            cantidades[i] = new NetworkVariable<int>(inicial,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);
        }

        indiceActivo.OnValueChanged += (viejo, nuevo) => EquiparVisual(nuevo);
        EquiparVisual(indiceActivo.Value);
    }

    public void Equipar(int indice)
    {
        if (!IsOwner || inputBloqueado) return;

        if (indice == indiceActivo.Value)
        {
            indiceActivo.Value = -1; // tocar el mismo boton de lo que tenes equipado lo desequipa
            return;
        }

        if (indice < 0 || indice >= items.Length) return;
        if (items[indice].item == null) return; // posición vacía, no hay nada que equipar
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
        weaponActual = null;

        if (indice < 0) return; // manos vacías

        var data = items[indice].item;
        if (data == null || data.prefabEnMano == null) return;

        instanciaActual = Instantiate(data.prefabEnMano, weaponHolder);
        weaponActual = instanciaActual.GetComponent<WeaponShoot>();

        if (weaponActual != null)
        {
            weaponActual.Init(weaponHolder, mirar, camaraJugador);
        }
    }
    public void Disparar()
    {
        if (!IsOwner || inputBloqueado) return;

        if (weaponActual != null)
        {
            weaponActual.Disparar();
        }
        else if (indiceActivo.Value == -1)
        {
            melee.Atacar();
        }
    }

    public void Recargar()
    {
        if (!IsOwner) return;
        if (weaponActual != null) weaponActual.Recargar();
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
        if (saldoSube.Value - monto < -1200) return false; // saldo negativo

        saldoSube.Value -= monto;
        return true;
    }

    [ServerRpc] public void IntentarPagarServerRpc(int monto)
    {
        PagarConSube(monto);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IntentarComprarRpc(int precio, int indiceSlot)
    {
        if (indiceSlot < 0 || indiceSlot >= items.Length) return;
        if (items[indiceSlot].item == null) return;

        if (!PagarConSube(precio)) return;

        var data = items[indiceSlot].item;
        if (data.esConsumible)
        {
            cantidades[indiceSlot].Value += 1;
        }
        else
        {
            cantidades[indiceSlot].Value = -1;
        }
    }
}
