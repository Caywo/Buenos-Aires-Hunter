using Unity.Netcode;
using UnityEngine;

public class ControladorDerrotaCooperativa : NetworkBehaviour
{
    private bool derrotaNotificada = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Health.OnCualquierMuerte += AlMorirEntidad;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        Health.OnCualquierMuerte -= AlMorirEntidad;
    }

    private void AlMorirEntidad(Health victima, ulong atacanteId)
    {
        if (!IsServer || !victima.EsJugador || derrotaNotificada) return;

        bool quedanJugadoresVivos = false;

        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                var health = cliente.PlayerObject.GetComponent<Health>();
                if (health != null && !health.EstaMuerto)
                {
                    quedanJugadoresVivos = true;
                    break;
                }
            }
        }

        if (!quedanJugadoresVivos)
        {
            derrotaNotificada = true;
            NotificarDerrotaEquipoClientRpc();
        }
    }

    [ClientRpc]
    private void NotificarDerrotaEquipoClientRpc()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            var ui = localPlayer.GetComponentInChildren<PlayerUI>(true);
            if (ui != null)
            {
                ui.MostrarPantallaDerrota();
            }
        }
    }
}