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
    private PlayerCombat combate;   // NUEVO (combate)
    private Health salud;           // NUEVO (combate)
    private Camera camaraJugador;

    // NUEVO (fix apuntado): pose base del weaponHolder y FOV de la cámara. Se guardan una sola vez
    // para restaurarlos al cambiar de ítem (si no, un arma nueva "hereda" la pose de apuntado).
    private Vector3 posBaseHolder;
    private float fovBase;

    public int indiceSube = -1;
    public NetworkVariable<int> saldoSube = new NetworkVariable<int>(-1200,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    public bool TieneSubeEquipada => indiceActivo.Value == indiceSube;

    // NUEVO (combate): acceso de solo lectura para PlayerCombat / HUD
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
        // NUEVO (fix apuntado): capturar la pose base ANTES de equipar nada
        if (weaponHolder != null) posBaseHolder = weaponHolder.localPosition;
        if (camaraJugador != null) fovBase = camaraJugador.fieldOfView;

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
        if (salud != null && salud.EstaMuerto) return; // NUEVO (combate)

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
        if (cantidades[indice].Value <= 0 && indiceActivo.Value == indice)
        {
            indiceActivo.Value = -1;
        }
    }

    private void EquiparVisual(int indice)
    {
        if (instanciaActual != null)
        {
            instanciaActual.SetActive(false); // NUEVO (fix apuntado): que no vuelva a animar en este frame
            Destroy(instanciaActual);
        }
        instanciaActual = null;
        weaponActual = null;

        // NUEVO (fix apuntado): si estabas apuntando, el arma anterior dejó el holder y el FOV en la pose
        // de apuntado. Se restauran ANTES de crear la nueva arma, que toma esos valores como su pose
        // "de cadera". También cubre guardar el arma o pasar a un ítem sin arma (SUBE), donde nadie más
        // restauraría el zoom.
        if (weaponHolder != null) weaponHolder.localPosition = posBaseHolder;
        if (IsOwner && camaraJugador != null) camaraJugador.fieldOfView = fovBase;

        if (indice >= 0 && indice < items.Length) // si es -1: manos vacías
        {
            var data = items[indice].item;
            if (data != null && data.prefabEnMano != null)
            {
                instanciaActual = Instantiate(data.prefabEnMano, weaponHolder);
                weaponActual = instanciaActual.GetComponent<WeaponShoot>();

                if (weaponActual != null)
                {
                    // NUEVO (combate): 4º parámetro esLocal
                    weaponActual.Init(weaponHolder, mirar, camaraJugador, IsOwner);
                }
            }
        }

        // NUEVO (combate): PlayerCombat se entera del cambio (cancela recargas en curso)
        OnArmaCambiada?.Invoke(weaponActual);
    }

    // CAMBIO (combate): ahora recibe si es una pulsación nueva (para armas semiautomáticas)
    // y el disparo real lo valida el servidor en PlayerCombat.
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

    public int GetCantidad(int indice) => cantidades[indice].Value;

    public void SetApuntando(bool apuntando)
    {
        if (!IsOwner) return;
        // Dejar de apuntar se permite siempre; empezar a apuntar, no (tienda, cuenta regresiva del duelo)
        if (apuntando && inputBloqueado) return;
        if (weaponActual != null) weaponActual.SetAiming(apuntando);
    }

    // ───────── NUEVO (modo duelo) ─────────

    /// <summary>Un slot está desbloqueado si su cantidad no es 0 (0 = bloqueado, -1 = desbloqueado, >0 = consumible).</summary>
    public bool EstaDesbloqueado(int indice)
    {
        if (cantidades == null || indice < 0 || indice >= cantidades.Length) return false;
        return cantidades[indice].Value != 0;
    }

    /// <summary>
    /// SOLO SERVIDOR. Deja desbloqueados únicamente los slots indicados y bloquea el resto.
    /// Sirve para que ambos jugadores del duelo tengan exactamente el mismo equipamiento,
    /// sin importar lo comprado o guardado en otros modos.
    /// </summary>
    public void ConfigurarEquipamientoFijo(int[] slotsDesbloqueados)
    {
        if (!IsServer || cantidades == null) return;

        for (int i = 0; i < cantidades.Length; i++)
        {
            if (System.Array.IndexOf(slotsDesbloqueados, i) >= 0)
            {
                var data = items[i].item;
                cantidades[i].Value = (data != null && data.esConsumible) ? data.cantidadInicial : -1;
            }
            else
            {
                cantidades[i].Value = 0;
            }
        }
    }

    /// <summary>SOLO SERVIDOR. Equipa un slot en el dueño sin pasar por el input (ej: al empezar una ronda).</summary>
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
        indiceActivo.Value = indice; // el dueño escribe su propia variable
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

    // GUARDADO DE JUGADOR
    public JugadorGuardado ObtenerDatosGuardado()
    {
        var lista = new System.Collections.Generic.List<ItemGuardado>();

        for (int i = 0; i < cantidades.Length; i++)
        {
            if (cantidades[i].Value != 0) // 0 = bloqueado, no hace falta guardarlo
            {
                lista.Add(new ItemGuardado { indiceSlot = i, cantidad = cantidades[i].Value });
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
            if (item.indiceSlot >= 0 && item.indiceSlot < cantidades.Length)
            {
                cantidades[item.indiceSlot].Value = item.cantidad;
            }
        }
    }
}
