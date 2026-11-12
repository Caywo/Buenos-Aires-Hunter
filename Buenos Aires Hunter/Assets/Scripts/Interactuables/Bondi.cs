using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bondi : Interactuable
{
    [SerializeField] private int precioPasaje = 150;
    [SerializeField] private string siguienteMapa = "MapaExtraccion2";
    [SerializeField] private bool esUltimoMapa = false;

    private readonly HashSet<ulong> jugadoresQuePagaron = new HashSet<ulong>();
    private bool cambiandoDeEscena = false;

    public override void InteractuarConItem(PlayerInventory jugador)
    {
        if (!IsServer || cambiandoDeEscena || jugador == null)
            return;

        ulong clientId = jugador.OwnerClientId;

        if (jugadoresQuePagaron.Contains(clientId))
            return;

        if (!jugador.PagarConSube(precioPasaje))
        {
            Debug.Log($"[Bondi] El cliente {clientId} no tiene suficiente saldo en la SUBE.");
            return;
        }

        jugadoresQuePagaron.Add(clientId);
        Debug.Log($"[Bondi] Jugador {clientId} pagó el pasaje. Registrados: {jugadoresQuePagaron.Count}");

        // Crear los parámetros de envío dirigidos únicamente al cliente que pagó
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        BloquearControlesJugadorClientRpc(rpcParams);
        VerificarCondicionDeEscape();
    }

    private void VerificarCondicionDeEscape()
    {
        int totalJugadoresConectadosYVivos = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                Health salud = client.PlayerObject.GetComponent<Health>();
                if (salud != null && !salud.EstaMuerto)
                {
                    totalJugadoresConectadosYVivos++;
                }
            }
        }

        Debug.Log($"[Bondi] Pagados: {jugadoresQuePagaron.Count} / Requeridos: {totalJugadoresConectadosYVivos}");

        if (jugadoresQuePagaron.Count >= totalJugadoresConectadosYVivos && totalJugadoresConectadosYVivos > 0)
        {
            cambiandoDeEscena = true;
            EjecutarEscapeGrupal();
        }
    }

    private void EjecutarEscapeGrupal()
    {
        LogrosJugador.NotificarExtraccionATodos();

        if (!esUltimoMapa)
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.GuardarPartida(siguienteMapa);

            NetworkManager.Singleton.SceneManager.LoadScene(
                siguienteMapa,
                LoadSceneMode.Single
            );
        }
        else
        {
            MostrarVictoriaFinalClientRpc();
        }
    }

    [ClientRpc]
    private void BloquearControlesJugadorClientRpc(ClientRpcParams clientRpcParams = default)
    {
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            var motor = localPlayer.GetComponent<PlayerMotor>();
            var mirar = localPlayer.GetComponent<PlayerLook>();
            var UI = localPlayer.GetComponentInChildren<PlayerUI>();

            if (motor != null) motor.inputBloqueado = true;
            if (mirar != null) mirar.inputBloqueado = true;

            if (UI != null)
            {
                bool esMultijugador = NetworkManager.Singleton.ConnectedClientsList.Count > 1;
                UI.ActualizarTexto(esMultijugador 
                    ? "Pasaje pagado. Esperando a tu compañero..." 
                    : "Pasaje pagado. Arrancando el colectivo...");
            }
        }
    }

    [ClientRpc]
    private void MostrarVictoriaFinalClientRpc()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerUI ui = FindAnyObjectByType<PlayerUI>();
        if (ui != null)
        {
            ui.MostrarPantallaVictoria(); 
        }
    }
}