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

    private NetworkVariable<int> indiceActivo = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<int>[] cantidades;

    private GameObject instanciaActual;
    private WeaponShoot weaponActual;

    private PlayerLook mirar;
    private PlayerCombat combate;
    private Health salud;
    private Camera camaraJugador;
    private MeleeAttack melee;

    public int indiceSube = -1;

    public NetworkVariable<int> saldoSube = new NetworkVariable<int>(
        -1200,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool TieneSubeEquipada =>
        indiceActivo.Value == indiceSube;

    // Acceso de solo lectura para PlayerCombat / HUD
    public int IndiceActivo =>
        indiceActivo.Value;

    public WeaponShoot ArmaActual =>
        weaponActual;

    public event System.Action<WeaponShoot> OnArmaCambiada;

    public bool inputBloqueado = false;


    // ============================================================
    // INICIALIZACIÓN
    // ============================================================

    private void Awake()
    {
        mirar = GetComponent<PlayerLook>();
        combate = GetComponent<PlayerCombat>();
        salud = GetComponent<Health>();
        camaraJugador = GetComponentInChildren<Camera>(true);

        melee = GetComponent<MeleeAttack>();

        if (melee != null)
            melee.Init(camaraJugador);
    }


    // ============================================================
    // NETWORK SPAWN
    // ============================================================

    public override void OnNetworkSpawn()
    {
        cantidades =
            new NetworkVariable<int>[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == null)
            {
                cantidades[i] =
                    new NetworkVariable<int>(
                        -1,
                        NetworkVariableReadPermission.Everyone,
                        NetworkVariableWritePermission.Server
                    );

                continue;
            }

            int inicial;

            if (!items[i].item.arrancaDesbloqueado)
            {
                inicial = 0;
            }
            else
            {
                inicial =
                    items[i].item.esConsumible
                    ? items[i].item.cantidadInicial
                    : -1;
            }

            cantidades[i] =
                new NetworkVariable<int>(
                    inicial,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server
                );
        }

        indiceActivo.OnValueChanged +=
            (viejo, nuevo) => EquiparVisual(nuevo);

        EquiparVisual(indiceActivo.Value);
    }


    // ============================================================
    // EQUIPAR
    // ============================================================

    public void Equipar(int indice)
    {
        if (!IsOwner || inputBloqueado)
            return;

        if (salud != null && salud.EstaMuerto)
            return;

        // Si apretamos nuevamente el mismo slot,
        // lo desequipamos.
        if (indice == indiceActivo.Value)
        {
            indiceActivo.Value = -1;
            return;
        }

        if (indice < 0 || indice >= items.Length)
            return;

        if (items[indice].item == null)
            return;

        if (cantidades[indice].Value == 0)
            return;

        indiceActivo.Value = indice;
    }


    // ============================================================
    // USAR CONSUMIBLE
    // ============================================================

    public void UsarObjetoActivo()
    {
        if (!IsOwner || inputBloqueado)
            return;

        if (salud != null && salud.EstaMuerto)
            return;

        int indice = indiceActivo.Value;

        if (indice < 0 || indice >= items.Length)
            return;

        ItemData data = items[indice].item;

        if (data == null)
            return;

        if (!data.esConsumible)
            return;

        if (cantidades[indice].Value <= 0)
            return;

        UsarServerRpc(indice);

        // Volvemos a las manos vacías después de usarlo.
        indiceActivo.Value = -1;
    }


    [ServerRpc]
    private void UsarServerRpc(int indice)
    {
        if (indice < 0 || indice >= items.Length)
            return;

        ItemData data = items[indice].item;

        if (data == null)
            return;

        if (!data.esConsumible)
            return;

        if (cantidades[indice].Value <= 0)
            return;

        // Consumimos una unidad.
        cantidades[indice].Value--;

        // ========================================================
        // WACHÍN
        // ========================================================

        if (data.esWachin)
        {
            SpawnWachin(data);
            return;
        }

       
    }


    // ============================================================
    // SPAWN DE WACHÍN
    // ============================================================

    private void SpawnWachin(ItemData data)
    {
        if (!IsServer)
            return;

        if (data.prefabUso == null)
        {
            Debug.LogError(
                "PlayerInventory: el ItemData '" +
                data.nombre +
                "' no tiene asignado prefabUso."
            );

            return;
        }

        Vector3 posicionSpawn =
            transform.position +
            transform.forward * 1.5f;

        Quaternion rotacionSpawn =
            Quaternion.LookRotation(
                transform.forward
            );

        GameObject wachin =
            Instantiate(
                data.prefabUso,
                posicionSpawn,
                rotacionSpawn
            );

        NetworkObject networkObject =
            wachin.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "PlayerInventory: el prefab de Wachín " +
                "no tiene NetworkObject."
            );

            Destroy(wachin);
            return;
        }

        networkObject.Spawn();

        Debug.Log(
            "Wachín creado por el jugador " +
            OwnerClientId
        );
    }


    // ============================================================
    // VISUAL DEL OBJETO EQUIPADO
    // ============================================================

    private void EquiparVisual(int indice)
    {
        if (instanciaActual != null)
            Destroy(instanciaActual);

        instanciaActual = null;
        weaponActual = null;

        // -1 significa manos vacías.
        if (indice >= 0 && indice < items.Length)
        {
            ItemData data =
                items[indice].item;

            if (data != null &&
                data.prefabEnMano != null)
            {
                instanciaActual =
                    Instantiate(
                        data.prefabEnMano,
                        weaponHolder
                    );

                weaponActual =
                    instanciaActual.GetComponent<WeaponShoot>();

                if (weaponActual != null)
                {
                    weaponActual.Init(
                        weaponHolder,
                        mirar,
                        camaraJugador,
                        IsOwner
                    );
                }
            }
        }

        // Avisamos a PlayerCombat del cambio.
        OnArmaCambiada?.Invoke(weaponActual);
    }


    // ============================================================
    // DISPARAR / USAR
    // ============================================================

    public void Disparar(bool pulsacionNueva)
    {
        if (!IsOwner || inputBloqueado)
            return;

        if (salud != null && salud.EstaMuerto)
            return;

        // --------------------------------------------------------
        // CONSUMIBLE EQUIPADO
        // --------------------------------------------------------

        if (indiceActivo.Value >= 0 &&
            indiceActivo.Value < items.Length)
        {
            ItemData data =
                items[indiceActivo.Value].item;

            if (data != null && data.esConsumible)
            {
                if (pulsacionNueva)
                    UsarObjetoActivo();

                return;
            }
        }

        // --------------------------------------------------------
        // ARMA EQUIPADA
        // --------------------------------------------------------

        if (weaponActual != null)
        {
            if (combate != null)
                combate.IntentarDisparar(
                    pulsacionNueva
                );
        }

        // --------------------------------------------------------
        // MANOS VACÍAS
        // --------------------------------------------------------

        else if (
            indiceActivo.Value == -1 &&
            pulsacionNueva
        )
        {
            if (melee != null)
                melee.Atacar();
        }
    }


    // ============================================================
    // RECARGAR
    // ============================================================

    public void Recargar()
    {
        if (!IsOwner || inputBloqueado)
            return;

        if (weaponActual != null &&
            combate != null)
        {
            combate.IntentarRecargar();
        }
    }


    // ============================================================
    // CANTIDADES
    // ============================================================

    public int GetCantidad(int indice)
    {
        if (cantidades == null)
            return 0;

        if (indice < 0 ||
            indice >= cantidades.Length)
            return 0;

        return cantidades[indice].Value;
    }


    // ============================================================
    // APUNTAR
    // ============================================================

    public void SetApuntando(bool apuntando)
    {
        if (!IsOwner)
            return;

        if (weaponActual != null)
            weaponActual.SetAiming(apuntando);
    }


    // ============================================================
    // SUBE
    // ============================================================

    public void AgregarSaldo(int cantidad)
    {
        if (!IsServer)
            return;

        saldoSube.Value += cantidad;
    }


    public bool PagarConSube(int monto)
    {
        if (!IsServer)
            return false;

        // La SUBE debe estar equipada.
        if (indiceActivo.Value != indiceSube)
            return false;

        // Permite saldo negativo hasta -1200.
        if (saldoSube.Value - monto < -1200)
            return false;

        saldoSube.Value -= monto;

        return true;
    }


    [ServerRpc]
    public void IntentarPagarServerRpc(int monto)
    {
        PagarConSube(monto);
    }


    // ============================================================
    // COMPRAR
    // ============================================================

    [Rpc(
        SendTo.Server,
        InvokePermission = RpcInvokePermission.Everyone
    )]
    public void IntentarComprarRpc(
        int precio,
        int indiceSlot
    )
    {
        if (indiceSlot < 0 ||
            indiceSlot >= items.Length)
            return;

        if (items[indiceSlot].item == null)
            return;

        if (!PagarConSube(precio))
            return;

        ItemData data =
            items[indiceSlot].item;

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