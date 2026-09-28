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

    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        inventario = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerUI == null)
        {
            playerUI = FindAnyObjectByType<PlayerUI>();
            if (playerUI == null) return;
        }
        playerUI.ActualizarTexto(string.Empty);
        objetivo = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distancia);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distancia, mask))
        {
            objetivo = hitInfo.collider.GetComponentInParent<Interactuable>();
            if (objetivo != null)
            {
                playerUI.ActualizarTexto(objetivo.mensaje);
            }
        }
    }

    public void Interactuar()
    {
        if (!IsOwner || objetivo == null) return;
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