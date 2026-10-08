using UnityEngine;
using Unity.Netcode;

public class PlayerInteract : NetworkBehaviour
{
    private Camera cam;
    [SerializeField] private float distancia = 3f;
    [SerializeField] private LayerMask mask = ~0;
    private PlayerInventory inventario;
    private Interactuable objetivo;
    private PlayerUI playerUI;
    private TiendaUI tiendaUI;
    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        inventario = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerUI == null) playerUI = FindAnyObjectByType<PlayerUI>();
        if (tiendaUI == null) tiendaUI = FindAnyObjectByType<TiendaUI>();

        // CAMBIO: el jugador puede existir en una escena sin HUD (ej: el menú principal,
        // donde se crea al hacer StartHost). Sin PlayerUI no hay nada que actualizar.
        if (playerUI == null) return;

        objetivo = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distancia);
        RaycastHit hitInfo;

        string texto = string.Empty;

        if (Physics.Raycast(ray, out hitInfo, distancia, mask))
        {
            objetivo = hitInfo.collider.GetComponentInParent<Interactuable>();
            if (objetivo != null)
            {
                texto = objetivo.mensaje;

                if (objetivo is Tienda && !inventario.TieneSubeEquipada)
                {
                    texto = "Necesitás la SUBE en mano";
                }
            }
        }

        playerUI.ActualizarTexto(texto);
    }

    public void Interactuar()
    {
        if (!IsOwner || objetivo == null) return;

        if (objetivo is Tienda)
        {
            if (!inventario.TieneSubeEquipada)
            {
                if (playerUI != null) playerUI.ActualizarTexto("Necesitás la SUBE en mano");
                return;
            }

            if (tiendaUI == null) return; // CAMBIO: guarda por si la escena no tiene TiendaUI
            tiendaUI.AbrirTienda(inventario);
            return;
        }

        IntentarInteractuarServerRpc(objetivo.NetworkObjectId);
    }

    [ServerRpc]
    private void IntentarInteractuarServerRpc(ulong objetoId)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objetoId, out NetworkObject netObj))
        {
            var interactuable = netObj.GetComponentInChildren<Interactuable>();
            if (interactuable != null)
            {
                interactuable.Interactuar();
                interactuable.InteractuarConItem(inventario);
            }
        }
    }
}