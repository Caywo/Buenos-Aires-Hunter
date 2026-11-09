
using UnityEngine;
using Unity.Netcode;

public class PlayerInteract : NetworkBehaviour
{
    private Camera cam;

    [SerializeField] private float distancia = 3f;
    [SerializeField] private LayerMask mask = ~0;

    private PlayerInventory inventario;
    private Interactuable objetivo;
    private RitualBoss ritual;

    private PlayerUI playerUI;
    private TiendaUI tiendaUI;
    private EnemySpawnerSupervivencia spawner;

    private PlayerDownState jugadorCaido;
    private PlayerDownState objetivoReanimacionEnCurso;

    void Start()
    {
        PlayerLook playerLook = GetComponent<PlayerLook>();
        if (playerLook != null)
            cam = playerLook.cam;

        inventario = GetComponent<PlayerInventory>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerUI == null)
            playerUI = FindAnyObjectByType<PlayerUI>();

        if (tiendaUI == null)
            tiendaUI = FindAnyObjectByType<TiendaUI>();

        if (playerUI == null || cam == null) return;

        objetivo = null;
        ritual = null;
        jugadorCaido = null;

        string texto = string.Empty;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distancia);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, distancia, mask))
        {
            // Primero comprobamos si estamos mirando la mesa ritual.
            ritual = hitInfo.collider.GetComponentInParent<RitualBoss>();

            if (ritual != null)
            {
                texto = ritual.TextoInteraccion();
            }
            else
            {
                // Comprobamos si estamos mirando un jugador caído.
                jugadorCaido = hitInfo.collider.GetComponentInParent<PlayerDownState>();

                if (jugadorCaido != null)
                {
                    Health vidaAjena = jugadorCaido.GetComponent<Health>();

                    if (vidaAjena != null && vidaAjena.EstaMuerto)
                    {
                        if (inventario == null || !inventario.TieneSubeEquipada)
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
                else
                {
                    // Si no es un jugador caído, buscamos un objeto interactuable.
                    objetivo = hitInfo.collider.GetComponentInParent<Interactuable>();

                    if (objetivo != null)
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

                            if (inventario == null || !inventario.TieneSubeEquipada)
                            {
                                texto = "Necesitás la SUBE en mano";
                            }
                        }
                    }
                }
            }
        }

        playerUI.ActualizarTexto(texto);
    }

    public void Interactuar()
    {
        if (!IsOwner) return;

        // Interacción con la mesa ritual.
        if (ritual != null)
        {
            if (ritual.PuedeIniciar)
                ritual.IntentarIniciar();

            return;
        }

        // La reanimación se gestiona manteniendo E.
        if (jugadorCaido != null) return;

        if (objetivo == null) return;

        // Interacción con la tienda.
        if (objetivo is Tienda)
        {
            if (spawner == null)
                spawner = FindAnyObjectByType<EnemySpawnerSupervivencia>();

            if (spawner != null && !spawner.rondaEnPausa.Value)
            {
                playerUI.ActualizarTexto("La tienda abre en la ronda de descanso");
                return;
            }

            if (inventario == null || !inventario.TieneSubeEquipada)
            {
                playerUI.ActualizarTexto("Necesitás la SUBE en mano");
                return;
            }

            if (tiendaUI != null)
                tiendaUI.AbrirTienda(inventario);

            return;
        }

        // Interacción normal con objetos.
        IntentarInteractuarServerRpc(objetivo.NetworkObjectId);
    }

    [ServerRpc]
    private void IntentarInteractuarServerRpc(ulong objetoId)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            objetoId, out NetworkObject netObj))
        {
            Interactuable interactuable =
                netObj.GetComponentInChildren<Interactuable>();

            if (interactuable != null)
            {
                interactuable.Interactuar();
                interactuable.InteractuarConItem(inventario);
            }
        }
    }

    // Se ejecuta cuando se presiona E.
    public void IniciarInteraccion()
    {
        if (!IsOwner) return;

        if (jugadorCaido == null) return;

        if (inventario == null || !inventario.TieneSubeEquipada)
            return;

        Health saludObjetivo = jugadorCaido.GetComponent<Health>();

        if (saludObjetivo != null && saludObjetivo.EstaMuerto)
        {
            objetivoReanimacionEnCurso = jugadorCaido;
            objetivoReanimacionEnCurso.IniciarReanimacion();
        }
    }

    // Se ejecuta cuando se suelta E.
    public void TerminarInteraccion()
    {
        if (!IsOwner) return;

        // Si estábamos reanimando, cancelamos esa reanimación.
        if (objetivoReanimacionEnCurso != null)
        {
            objetivoReanimacionEnCurso.CancelarReanimacion();
            objetivoReanimacionEnCurso = null;
            return;
        }

        // Si no estábamos reanimando, realizamos la interacción normal.
        Interactuar();
    }
}
