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
    private PlayerMirar mirar;
    private Camera camaraJugador;

    void Awake()
    {
        mirar = GetComponent<PlayerMirar>();
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
        if (!IsOwner) return;
        if (weaponActual != null) weaponActual.Disparar();
    }
    public int GetCantidad(int indice) => cantidades[indice].Value;
}
