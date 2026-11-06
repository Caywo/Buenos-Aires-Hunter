using UnityEngine;
using Unity.Netcode;

public class PlayerInteract : NetworkBehaviour
{
    private Camera cam;
    [SerializeField] private float distancia = 3f;
    [SerializeField] private LayerMask mask = ~0;
    private PlayerInventory inventario;
    private Interactuable objetivo;
    private RitualBoss ritual; // NUEVO: mesa ritual que está mirando el jugador
    private PlayerUI playerUI;
    private TiendaUI tiendaUI;
    private EnemySpawnerSupervivencia spawner; // para el modo supervivencia
    private PlayerDownState jugadorCaido;
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
        ritual = null; // NUEVO

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distancia);
        RaycastHit hitInfo;

        string texto = string.Empty;

        if (Physics.Raycast(ray, out hitInfo, distancia, mask))
        {
            // NUEVO: la mesa ritual se revisa primero (no es un Interactuable).
            ritual = hitInfo.collider.GetComponentInParent<RitualBoss>();

            if (ritual != null)
            {
                texto = ritual.TextoInteraccion();
            }
            else
            {
                objetivo = hitInfo.collider.GetComponentInParent<Interactuable>();
            }

            if (objetivo != null)
            {
                texto = objetivo.mensaje;

                if (objetivo is Tienda)
                {
                    if (spawner == null) spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

                    if (spawner != null && !spawner.rondaEnPausa.Value)
                    {
                        texto = "La tienda abre en la ronda de descanso";
                    }
                    if (!inventario.TieneSubeEquipada) texto = "Necesitás la SUBE en mano";
                }
                jugadorCaido = hitInfo.collider.GetComponentInParent<PlayerDownState>();

                if (jugadorCaido != null)
                {
                    var vidaAjena = jugadorCaido.GetComponent<Health>();
                    texto = vidaAjena != null && vidaAjena.EstaMuerto ? "Mantené E para revivir" : string.Empty;
                }
            }
        }
        playerUI.ActualizarTexto(texto);
    }

    public void Interactuar()
    {
        if (!IsOwner) return;

        // NUEVO: iniciar el ritual del boss secreto
        if (ritual != null)
        {
            if (ritual.PuedeIniciar)
                ritual.IntentarIniciar();

            return;
        }

        if (objetivo == null) return;

        if (objetivo is Tienda)
        {
            if (spawner == null) spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

            if (spawner != null && !spawner.rondaEnPausa.Value)
            {
                playerUI.ActualizarTexto("La tienda abre en la ronda de descanso");
                return;
            }

            if (!inventario.TieneSubeEquipada)
            {
                playerUI.ActualizarTexto("Necesitás la SUBE en mano");
                return;
            }

            tiendaUI.AbrirTienda(inventario);
            return;
        }
        if (jugadorCaido != null)
        {
            jugadorCaido.IntentarRevivirRpc(OwnerClientId);
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