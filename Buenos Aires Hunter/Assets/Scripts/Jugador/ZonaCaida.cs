using Unity.Netcode;
using UnityEngine;

public class ZonaCaida : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Obtener la entidad de red del jugador
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        // Si estamos en el Modo Duelo, delegamos el respawn al DuelManager
        if (DuelManager.Instance != null)
        {
            DuelManager.Instance.RespawnearJugadorEnCaida(netObj.OwnerClientId);
            return;
        }

        // Respaldo (Fallback) para Singleplayer o mapas de prueba sin DuelManager:
        var combat = other.GetComponentInParent<PlayerCombat>();
        if (combat != null)
        {
            combat.Respawnear();
        }
    }
}