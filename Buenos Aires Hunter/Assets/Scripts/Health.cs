using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Vida sincronizada. Sirve tanto para jugadores como para enemigos.
/// Solo el servidor puede modificar la vida.
/// </summary>
public class Health : NetworkBehaviour, IDamageable
{
    [SerializeField] private float vidaMaxima = 100f;

    [Tooltip("Marcar en el prefab del jugador. Se usa para aplicar la regla de PvP.")]
    [SerializeField] private bool esJugador = false;

    [Tooltip("Marcar en enemigos: al morir se hace Despawn del NetworkObject.")]
    [SerializeField] private bool despawnAlMorir = false;
    [SerializeField] private float retrasoDespawn = 0.5f;

    private readonly NetworkVariable<float> vidaActual = new NetworkVariable<float>(100f);
    private readonly NetworkVariable<bool> muerto = new NetworkVariable<bool>(false);

    public float VidaActual => vidaActual.Value;
    public float VidaMaxima => vidaMaxima;
    public bool EstaMuerto => muerto.Value;
    public bool EsJugador => esJugador;

    /// <summary>(vidaActual, vidaMaxima). Se dispara en todos los peers.</summary>
    public event Action<float, float> OnVidaCambiada;

    /// <summary>true = murió, false = revivió. Se dispara en todos los peers.</summary>
    public event Action<bool> OnMuerteCambiada;

    /// <summary>SOLO SERVIDOR. Parámetro: clientId del asesino (o AtacanteNeutral).</summary>
    public event Action<ulong> OnMuerto;

    /// <summary>SOLO SERVIDOR. Ideal para que un DuelManager / marcador cuente kills.</summary>
    public static event Action<Health, ulong> OnCualquierMuerte;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaActual.Value = vidaMaxima;
            muerto.Value = false;
        }

        vidaActual.OnValueChanged += AlCambiarVida;
        muerto.OnValueChanged += AlCambiarMuerte;
    }

    public override void OnNetworkDespawn()
    {
        vidaActual.OnValueChanged -= AlCambiarVida;
        muerto.OnValueChanged -= AlCambiarMuerte;
    }

    private void AlCambiarVida(float anterior, float nueva) => OnVidaCambiada?.Invoke(nueva, vidaMaxima);
    private void AlCambiarMuerte(bool anterior, bool nueva) => OnMuerteCambiada?.Invoke(nueva);

    public void TakeDamage(float dano, ulong atacanteId)
    {
        if (!IsServer) return;
        if (muerto.Value || dano <= 0f) return;

        // Regla PvP: un jugador solo recibe daño de otros jugadores si el PvP está activo.
        if (esJugador && atacanteId != ReglasCombate.AtacanteNeutral && !ReglasCombate.PvPActivo)
            return;

        vidaActual.Value = Mathf.Max(0f, vidaActual.Value - dano);

        if (vidaActual.Value <= 0f)
            Morir(atacanteId);
    }

    public void Curar(float cantidad)
    {
        if (!IsServer || muerto.Value || cantidad <= 0f) return;
        vidaActual.Value = Mathf.Min(vidaMaxima, vidaActual.Value + cantidad);
    }

    public void Revivir()
    {
        if (!IsServer) return;
        vidaActual.Value = vidaMaxima;
        muerto.Value = false;
    }

    private void Morir(ulong atacanteId)
    {
        muerto.Value = true;
        OnMuerto?.Invoke(atacanteId);
        OnCualquierMuerte?.Invoke(this, atacanteId);

        if (despawnAlMorir)
            StartCoroutine(DespawnConRetraso());
    }

    private IEnumerator DespawnConRetraso()
    {
        yield return new WaitForSeconds(retrasoDespawn);
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
