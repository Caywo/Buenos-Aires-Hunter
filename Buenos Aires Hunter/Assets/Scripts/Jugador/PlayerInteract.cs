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
    private EnemySpawnerSupervivencia spawner; // para el modo supervivencia
    private PlayerDownState jugadorCaido;
    private PlayerDownState objetivoReanimacionEnCurso;
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
        jugadorCaido = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distancia);
        RaycastHit hitInfo;

        string texto = string.Empty;

        if (Physics.Raycast(ray, out hitInfo, distancia, mask))
        {
            objetivo = hitInfo.collider.GetComponentInParent<Interactuable>();
            jugadorCaido = hitInfo.collider.GetComponentInParent<PlayerDownState>();

            // Si estamos apuntando a un jugador caído.
            if (jugadorCaido != null)
            {
                var vidaAjena = jugadorCaido.GetComponent<Health>();

                if (vidaAjena != null && vidaAjena.EstaMuerto)
                {
                    if (!inventario.TieneSubeEquipada)
                    {
                        texto = "Necesitás equipar la SUBE para revivir";
                    }
                    else if (jugadorCaido.IntentandoRevivir)
                    {
                        texto = $"Reanimando... {jugadorCaido.TiempoRestante:F1}s";
                    }
                    else
                    {
                        texto = "Mantené E para revivir $500";
                    }
                }
            }

            // Si no estamos apuntando a un jugador caído,
            // mostramos los mensajes de los objetos interactuables.
            if (jugadorCaido == null && objetivo != null)
            {
                texto = objetivo.mensaje;

                if (objetivo is Tienda)
                {
                    if (spawner == null)
                        spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

                    if (spawner != null && !spawner.rondaEnPausa.Value)
                    {
                        texto = "La tienda abre en la ronda de descanso";
                    }

                    if (!inventario.TieneSubeEquipada)
                    {
                        texto = "Necesitás la SUBE en mano";
                    }
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
        if (jugadorCaido != null) return;
        

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
    public void IniciarInteraccion()
    {
        if (!IsOwner) return;

        if (jugadorCaido != null)
        {
            if (!inventario.TieneSubeEquipada) return;

            var saludObjetivo = jugadorCaido.GetComponent<Health>();

            if (saludObjetivo != null && saludObjetivo.EstaMuerto)
            {
                objetivoReanimacionEnCurso = jugadorCaido;
                objetivoReanimacionEnCurso.IniciarReanimacion();
            }
        }
    }

    public void TerminarInteraccion()
    {
        if (!IsOwner) return;

        // Cancelamos sobre el mismo jugador al que empezamos a reanimar,
        // aunque hayamos movido la mira.
        if (objetivoReanimacionEnCurso != null)
        {
            objetivoReanimacionEnCurso.CancelarReanimacion();
            objetivoReanimacionEnCurso = null;
            return;
        }

        Interactuar();
    }
}