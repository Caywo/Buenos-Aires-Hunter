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

    private NetworkList<int> cantidades = new NetworkList<int>(
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);

    private GameObject instanciaActual;
    private WeaponShoot weaponActual;
    private PlayerLook mirar;
    private PlayerCombat combate;
    private Health salud;
    private Camera camaraJugador;

    private Vector3 posBaseHolder;
    private float fovBase;

    public int indiceSube = -1;
    public NetworkVariable<int> saldoSube = new NetworkVariable<int>(-1200,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public bool TieneSubeEquipada => indiceActivo.Value == indiceSube;

    public int IndiceActivo => indiceActivo.Value;
    public WeaponShoot ArmaActual => weaponActual;

    public event System.Action<WeaponShoot> OnArmaCambiada;
    private MeleeAttack melee;
    public bool inputBloqueado = false;

    void Awake()
    {
        mirar = GetComponent<PlayerLook>();
        combate = GetComponent<PlayerCombat>();
        salud = GetComponent<Health>();
        camaraJugador = GetComponentInChildren<Camera>(true);
        melee = GetComponent<MeleeAttack>();
        melee.Init(camaraJugador);
    }

    public override void OnNetworkSpawn()
    {
        if (weaponHolder != null) posBaseHolder = weaponHolder.localPosition;
        if (camaraJugador != null) fovBase = camaraJugador.fieldOfView;

        if (IsServer) // solo el server llena la lista; se sincroniza sola a los clientes
        {
            cantidades.Clear();

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].item == null)
                {
                    cantidades.Add(-1);
                    continue;
                }

                int inicial;
                if (!items[i].item.arrancaDesbloqueado)
                {
                    inicial = 0;
                }
                else
                {
                    inicial = items[i].item.esConsumible ? items[i].item.cantidadInicial : -1;
                }

                cantidades.Add(inicial);
            }
        }

        indiceActivo.OnValueChanged += (viejo, nuevo) => EquiparVisual(nuevo);
        EquiparVisual(indiceActivo.Value);
    }

    public void Equipar(int indice)
    {
        if (!IsOwner || inputBloqueado) return;
        if (salud != null && salud.EstaMuerto) return;

        if (indice == indiceActivo.Value)
        {
            indiceActivo.Value = -1;
            return;
        }
        if (indice < 0 || indice >= items.Length) return;
        if (items[indice].item == null) return;
        if (cantidades[indice] == 0) return;

        indiceActivo.Value = indice;


    }
    public void UsarWachin()
    {
        if (!IsOwner || inputBloqueado) return;
        if (salud != null && salud.EstaMuerto) return;

        if (indiceActivo.Value < 0) return;
        UsarServerRpc(indiceActivo.Value);
    }

    [ServerRpc]
    private void UsarServerRpc(int indice)
    {
        var data = items[indice].item;
        if (!data.esConsumible) return;
        if (cantidades[indice] <= 0) return;

        cantidades[indice] = cantidades[indice] - 1;
        if (data.prefabUso != null)
        {
            Vector3 posicion = transform.position + transform.forward * 1.5f;
            GameObject instancia = Instantiate(data.prefabUso, posicion, Quaternion.identity);

            NetworkObject netObj = instancia.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
        indiceActivo.Value = -1;
    }

    private void EquiparVisual(int indice)
    {
        if (instanciaActual != null)
        {
            instanciaActual.SetActive(false);
            Destroy(instanciaActual);
        }
        instanciaActual = null;
        weaponActual = null;

        if (weaponHolder != null) weaponHolder.localPosition = posBaseHolder;
        if (IsOwner && camaraJugador != null) camaraJugador.fieldOfView = fovBase;

        if (indice >= 0 && indice < items.Length)
        {
            var data = items[indice].item;    
            if (data != null && data.prefabEnMano != null)
            {
                instanciaActual = Instantiate(data.prefabEnMano, weaponHolder);
                weaponActual = instanciaActual.GetComponent<WeaponShoot>();

                if (weaponActual != null)
                {
                    weaponActual.Init(weaponHolder, mirar, camaraJugador, IsOwner);
                }
            }
        }
        else if (prefabManos != null)
        {
            instanciaActual = Instantiate(prefabManos, weaponHolder);
        }

        OnArmaCambiada?.Invoke(weaponActual);
    }

    public GameObject prefabManos;

    public void Disparar(bool pulsacionNueva)
    {
        if (!IsOwner || inputBloqueado) return;
        if (salud != null && salud.EstaMuerto) return;

        if (weaponActual != null)
        {
            if (combate != null) combate.IntentarDisparar(pulsacionNueva);
        }
        else if (indiceActivo.Value == -1 && pulsacionNueva)
        {
            melee.Atacar();
        }
        else if (indiceActivo.Value >= 0 && items[indiceActivo.Value].item.esConsumible && pulsacionNueva)
        {
            UsarWachin();
        }
    }

    public void Recargar()
    {
        if (!IsOwner || inputBloqueado) return;
        if (weaponActual != null && combate != null) combate.IntentarRecargar();
    }

    public int GetCantidad(int indice) => cantidades[indice];

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
        if (indiceActivo.Value != indiceSube) return false;
        if (saldoSube.Value - monto < -1200) return false;

        saldoSube.Value -= monto;
        return true;
    }

    [ServerRpc]
    public void IntentarPagarServerRpc(int monto)
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
            cantidades[indiceSlot] = cantidades[indiceSlot] + 1;
        }
        else
        {
            cantidades[indiceSlot] = -1;
        }

    }

    public JugadorGuardado ObtenerDatosGuardado()
    {
        var lista = new System.Collections.Generic.List<ItemGuardado>();

        for (int i = 0; i < cantidades.Count; i++)
        {
            if (cantidades[i] != 0)
            {
                lista.Add(new ItemGuardado { indiceSlot = i, cantidad = cantidades[i] });
            }
        }

        return new JugadorGuardado
        {
            saldoSube = saldoSube.Value,
            items = lista.ToArray()
        };
    }

    public void AplicarDatosGuardado(JugadorGuardado datos)
    {
        if (!IsServer || datos == null) return;

        saldoSube.Value = datos.saldoSube;

        foreach (var item in datos.items)
        {
            if (item.indiceSlot >= 0 && item.indiceSlot < cantidades.Count)
            {
                cantidades[item.indiceSlot] = item.cantidad;
            }
        }
    }
}