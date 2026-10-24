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
    public GameObject[] slots;

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

        if (IsServer)
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

        cantidades.OnListChanged += AlCambiarCantidades;
        ActualizarEstadoInicialSlots();

        indiceActivo.OnValueChanged += (viejo, nuevo) => EquiparVisual(nuevo);
        EquiparVisual(indiceActivo.Value);
    }

    public override void OnNetworkDespawn()
    {
        cantidades.OnListChanged -= AlCambiarCantidades;
    }

    // Reacciona a cualquier cambio de cantidad (comprar, usar, etc.) y actualiza el slot correspondiente.
    // Corre en todos los peers que reciben la sincronización, pero solo el dueño toca su propia UI.
    private void AlCambiarCantidades(NetworkListEvent<int> cambio)
    {
        if (!IsOwner) return;
        if (slots == null || cambio.Index < 0 || cambio.Index >= slots.Length) return;

        var slotUI = slots[cambio.Index] != null ? slots[cambio.Index].GetComponent<SlotUI>() : null;
        if (slotUI == null) return;

        // No pisa el estado "Seleccionado" del slot actualmente equipado
        if (indiceActivo.Value == cambio.Index) return;

        slotUI.SetEstado(cambio.Value == 0 ? SlotUI.Estado.Bloqueado : SlotUI.Estado.Desbloqueado);
    }

    private void ActualizarEstadoInicialSlots()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length && i < cantidades.Count; i++)
        {
            var slotUI = slots[i] != null ? slots[i].GetComponent<SlotUI>() : null;
            if (slotUI == null) continue;

            slotUI.SetEstado(cantidades[i] == 0 ? SlotUI.Estado.Bloqueado : SlotUI.Estado.Desbloqueado);
        }
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
        if (cantidades[indice] <= 0 && indiceActivo.Value == indice)
        {
            indiceActivo.Value = -1;
        }
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

        if (slots != null)
        {
            foreach (var slotObj in slots)
            {
                var slotUI = slotObj != null ? slotObj.GetComponent<SlotUI>() : null;
                if (slotUI != null && slotUI.GetEstado() == SlotUI.Estado.Seleccionado)
                {
                    slotUI.SetEstado(SlotUI.Estado.Desbloqueado);
                }
            }
        }

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

            if (slots != null && indice < slots.Length)
            {
                var slotUI = slots[indice] != null ? slots[indice].GetComponent<SlotUI>() : null;
                slotUI?.SetEstado(SlotUI.Estado.Seleccionado);
            }
        }

        OnArmaCambiada?.Invoke(weaponActual);
    }

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
            UsarServerRpc(indiceActivo.Value);
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
        if (apuntando && inputBloqueado) return;
        if (weaponActual != null) weaponActual.SetAiming(apuntando);
    }

    public bool EstaDesbloqueado(int indice)
    {
        if (cantidades == null || indice < 0 || indice >= cantidades.Count) return false;
        return cantidades[indice] != 0;
    }

    public void ConfigurarEquipamientoFijo(int[] slotsDesbloqueados)
    {
        if (!IsServer || cantidades == null) return;

        for (int i = 0; i < cantidades.Count; i++)
        {
            if (System.Array.IndexOf(slotsDesbloqueados, i) >= 0)
            {
                var data = items[i].item;
                cantidades[i] = (data != null && data.esConsumible) ? data.cantidadInicial : -1;
            }
            else
            {
                cantidades[i] = 0;
            }
        }
    }

    public void ForzarEquipar(int indice)
    {
        if (!IsServer) return;

        ForzarEquiparClientRpc(indice, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void ForzarEquiparClientRpc(int indice, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        indiceActivo.Value = indice;
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
        // La UI se actualiza sola en el dueño, vía AlCambiarCantidades (OnListChanged)
    }

    // --GUARDADO JUGADOR--
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