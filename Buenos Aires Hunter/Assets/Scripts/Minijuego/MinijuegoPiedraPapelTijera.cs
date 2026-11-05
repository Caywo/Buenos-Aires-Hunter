using System;
using Unity.Netcode;
using UnityEngine;

public enum Jugada
{
    Piedra = 0,
    Papel = 1,
    Tijera = 2
}

/// <summary>
/// Piedra, papel o tijera con Horacio, al mejor de 3. Va en el prefab del jugador. Servidor autoritativo.
/// </summary>
public class MinijuegoPiedraPapelTijera : NetworkBehaviour
{
    [Header("Reglas")]
    [SerializeField] private int victoriasNecesarias = 2;

    // Solo servidor
    private bool partidaActiva;
    private int puntosJugador;
    private int puntosVendedor;

    // Eventos para la UI (se disparan en el dueño).
    public event Action<int, int> OnInicio;                         // puntosJugador, puntosVendedor
    public event Action<int, int, int, int, int, bool> OnRonda;     // jugadaJugador, jugadaVendedor, resultado (1 gana, 0 empate, -1 pierde), puntosJugador, puntosVendedor, terminada

    /// <summary>SOLO SERVIDOR. Se dispara al terminar una partida (true = ganó el jugador). Acá se enganchan los precios más adelante.</summary>
    public event Action<bool> OnPartidaTerminadaServidor;

    public int VictoriasNecesarias => victoriasNecesarias;

    // ───────────────────────── API LOCAL (la usa la UI) ─────────────────────────

    public void Iniciar()
    {
        if (IsOwner)
            IniciarServerRpc();
    }

    public void Jugar(Jugada jugada)
    {
        if (IsOwner)
            JugarServerRpc((int)jugada);
    }

    // ───────────────────────── SERVIDOR ─────────────────────────

    [ServerRpc]
    private void IniciarServerRpc()
    {
        // Si ya había una partida abierta (cerró la ventana), se retoma donde quedó.
        if (!partidaActiva)
        {
            partidaActiva = true;
            puntosJugador = 0;
            puntosVendedor = 0;
        }

        InicioClientRpc(puntosJugador, puntosVendedor, AlDueno());
    }

    [ServerRpc]
    private void JugarServerRpc(int jugada)
    {
        if (jugada < 0 || jugada > 2 || !partidaActiva)
            return;

        // Horacio elige DESPUÉS de recibir la jugada del jugador.
        int vendedor = UnityEngine.Random.Range(0, 3);

        // 1 = gana el jugador, 0 = empate, -1 = gana Horacio
        int diferencia = (jugada - vendedor + 3) % 3;
        int resultado = diferencia == 0 ? 0 : diferencia == 1 ? 1 : -1;

        if (resultado == 1)
            puntosJugador++;
        else if (resultado == -1)
            puntosVendedor++;

        bool terminada = puntosJugador >= victoriasNecesarias || puntosVendedor >= victoriasNecesarias;

        int pj = puntosJugador;
        int pv = puntosVendedor;

        if (terminada)
        {
            bool gano = pj > pv;

            partidaActiva = false;
            puntosJugador = 0;
            puntosVendedor = 0;

            OnPartidaTerminadaServidor?.Invoke(gano);
        }

        RondaClientRpc(jugada, vendedor, resultado, pj, pv, terminada, AlDueno());
    }

    private ClientRpcParams AlDueno()
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };
    }

    // ───────────────────────── CLIENTE ─────────────────────────

    [ClientRpc]
    private void InicioClientRpc(int pj, int pv, ClientRpcParams rpcParams = default)
    {
        OnInicio?.Invoke(pj, pv);
    }

    [ClientRpc]
    private void RondaClientRpc(int jugadaJugador, int jugadaVendedor, int resultado, int pj, int pv,
                                bool terminada, ClientRpcParams rpcParams = default)
    {
        OnRonda?.Invoke(jugadaJugador, jugadaVendedor, resultado, pj, pv, terminada);
    }
}
