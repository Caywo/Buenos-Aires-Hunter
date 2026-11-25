using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Va en el prefab del jugador. Es la forma de pedir un aviso en pantalla:
/// - Desde el SERVIDOR: EnviarAviso(...) lo muestra solo al jugador afectado.
/// - Desde el CLIENTE (por ejemplo una pantalla de código): PlayerAvisos.Local?.MostrarLocal(...).
/// </summary>
public class PlayerAvisos : NetworkBehaviour
{
    [SerializeField] private CartelAvisoUI cartel;

    /// <summary>El PlayerAvisos del jugador de esta computadora.</summary>
    public static PlayerAvisos Local { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        Local = null;
    }

    public override void OnNetworkSpawn()
    {
        // Cada jugador ve solo su propio cartel.
        if (cartel != null)
            cartel.gameObject.SetActive(IsOwner);

        if (IsOwner)
            Local = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this)
            Local = null;
    }

    /// <summary>Muestra el aviso en esta computadora (solo funciona en el dueño del jugador).</summary>
    public void MostrarLocal(string mensaje, string titulo = null)
    {
        if (!IsOwner || cartel == null)
            return;

        cartel.Mostrar(mensaje, titulo);
    }

    /// <summary>Pide el aviso para el dueño de este jugador. Se puede llamar desde el servidor o desde el dueño.</summary>
    public void EnviarAviso(string mensaje, string titulo = null)
    {
        if (IsServer)
            MostrarAvisoRpc(mensaje, titulo ?? string.Empty);
        else
            MostrarLocal(mensaje, titulo);
    }

    [Rpc(SendTo.Owner)]
    private void MostrarAvisoRpc(string mensaje, string titulo)
    {
        MostrarLocal(mensaje, titulo);
    }

    [ContextMenu("Probar aviso")]
    private void Probar()
    {
        EnviarAviso("Texto de prueba. Si lo ves, el cartel funciona.");
    }
}