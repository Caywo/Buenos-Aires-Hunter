
using Unity.Netcode;
using UnityEngine;

public class MunicionPickup : Interactuable
{
    [Header("Munición por tipo de arma")]
    [SerializeField] private int municionPistola = 30;
    [SerializeField] private int municionEscopeta = 12;
    [SerializeField] private int municionRifle = 45;

    private bool recogido;

    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer || recogido || jugador == null)
            return;

        PlayerCombat combate = jugador.GetComponent<PlayerCombat>();

        if (combate == null)
            return;

        int armasRecargadas = 0;

        for (int i = 0; i < jugador.items.Length; i++)
        {
            ItemData data = jugador.items[i].item;

            if (data == null || data.prefabEnMano == null)
                continue;

            // Comprobar que el slot contiene un arma.
            WeaponShoot arma = data.prefabEnMano.GetComponent<WeaponShoot>();

            if (arma == null)
                continue;

            // No dar munición a armas que el jugador no tiene.
            if (!jugador.EstaDesbloqueado(i))
                continue;

            int cantidad = ObtenerMunicion(data.tipoArma);

            if (cantidad <= 0)
                continue;

            combate.AgregarMunicion(i, cantidad);
            armasRecargadas++;
        }

        if (armasRecargadas == 0)
            return;

        recogido = true;
        DestruirPickup();
    }

    private int ObtenerMunicion(TipoArma tipo)
    {
        switch (tipo)
        {
            case TipoArma.Pistola:
                return municionPistola;

            case TipoArma.Escopeta:
                return municionEscopeta;

            case TipoArma.Rifle:
                return municionRifle;
            default:
                return 0;
        }
    }

    private void DestruirPickup()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
