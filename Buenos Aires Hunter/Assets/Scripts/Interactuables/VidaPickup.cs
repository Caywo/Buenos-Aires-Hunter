
using Unity.Netcode;
using UnityEngine;

public class VidaPickup : Interactuable
{
    [Header("Curación")]
    [SerializeField] private float cantidadCuracion = 40f;

    private bool recogido;

    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer || recogido || jugador == null)
            return;

        Health salud = jugador.GetComponent<Health>();

        if (salud == null || salud.EstaMuerto)
            return;

        // No consumir el botiquín si el jugador ya tiene toda la vida.
        if (salud.VidaActual >= salud.VidaMaxima)
            return;

        salud.Curar(cantidadCuracion);

        recogido = true;
        DestruirPickup();
    }

    private void DestruirPickup()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            Destroy(gameObject);
    }
}
