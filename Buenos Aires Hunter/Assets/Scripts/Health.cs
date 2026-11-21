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
    // Vida máxima real: la del prefab, multiplicada por la dificultad en los enemigos.
    private readonly NetworkVariable<float> vidaMaximaEfectiva = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<bool> muerto = new NetworkVariable<bool>(false);

    public float VidaActual => vidaActual.Value;
    public float VidaMaxima => vidaMaximaEfectiva.Value > 0f ? vidaMaximaEfectiva.Value : vidaMaxima;
    public bool EstaMuerto => muerto.Value;

    /// <summary>True si este Health pertenece a un jugador (sirve para distinguir jugadores de enemigos).</summary>
    public bool EsJugador => esJugador;

    /// <summary>(vidaActual, vidaMaxima). Se dispara en todos los peers.</summary>
    public event Action<float, float> OnVidaCambiada;

    /// <summary>true = murió, false = revivió. Se dispara en todos los peers.</summary>
    public event Action<bool> OnMuerteCambiada;

    /// <summary>SOLO SERVIDOR. Parámetro: clientId del asesino (o AtacanteNeutral).</summary>
    public event Action<ulong> OnMuerto;

    /// <summary>SOLO SERVIDOR. Ideal para que un DuelManager / marcador cuente kills.</summary>
    public static event Action<Health, ulong> OnCualquierMuerte;

    private void Start()
    {
        OnMuerteCambiada += ManejarMuerteLocal;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        OnMuerteCambiada -= ManejarMuerteLocal;
    }

    private void ManejarMuerteLocal(bool estaMuerto)
    {
        if (!esJugador) return;

        bool esLocal = IsOwner || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

        if (esLocal)
        {
            // Inmoviliza al jugador si murió (esperando reanimación) o le devuelve el control si fue revivido
            BloquearControlesLocalmente(estaMuerto);
        }
    }

    private void BloquearControlesLocalmente(bool bloquear)
    {
        var motor = GetComponent<PlayerMotor>();
        if (motor != null) motor.inputBloqueado = bloquear;

        var look = GetComponent<PlayerLook>();
        if (look != null) look.inputBloqueado = bloquear;

        var combate = GetComponent<PlayerCombat>();
        if (combate != null) combate.enabled = !bloquear;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            float maxima = vidaMaxima;

            // La dificultad escala la vida de los enemigos (no la de los jugadores).
            if (!esJugador)
                maxima *= Dificultad.Perfil.vidaEnemigos;

            vidaMaximaEfectiva.Value = maxima;
            vidaActual.Value = maxima;
            muerto.Value = false;

            if (!esJugador)
                Dificultad.AplicarAEnemigo(gameObject);
        }

        vidaActual.OnValueChanged += AlCambiarVida;
        muerto.OnValueChanged += AlCambiarMuerte;
    }

    public override void OnNetworkDespawn()
    {
        vidaActual.OnValueChanged -= AlCambiarVida;
        muerto.OnValueChanged -= AlCambiarMuerte;
    }

    private void AlCambiarVida(float anterior, float nueva) => OnVidaCambiada?.Invoke(nueva, VidaMaxima);
    private void AlCambiarMuerte(bool anterior, bool nueva) => OnMuerteCambiada?.Invoke(nueva);

    public void TakeDamage(float dano, ulong atacanteId)
    {
        if (!IsServer) return;
        if (muerto.Value || dano <= 0f) return;

        // Regla PvP: un jugador solo recibe daño de otros jugadores si el PvP está activo.
        if (esJugador && atacanteId != ReglasCombate.AtacanteNeutral && !ReglasCombate.PvPActivo)
            return;

        // Dificultad: el daño que los enemigos hacen a los jugadores se escala.
        if (esJugador && EsDanoDeEnemigo(atacanteId))
            dano *= Dificultad.Perfil.danoEnemigos;

        vidaActual.Value = Mathf.Max(0f, vidaActual.Value - dano);
        Debug.Log($"[Health] {name} recibió {dano} de daño (atacante {atacanteId}). Vida: {vidaActual.Value}/{VidaMaxima}");

        if (vidaActual.Value <= 0f)
            Morir(atacanteId);
    }

    // Daño que NO viene de otro jugador: el atacante neutral o cualquier enemigo.
    private bool EsDanoDeEnemigo(ulong atacanteId)
    {
        if (atacanteId == ReglasCombate.AtacanteNeutral)
            return true;

        return NetworkManager == null || !NetworkManager.ConnectedClients.ContainsKey(atacanteId);
    }

    public void Curar(float cantidad)
    {
        if (!IsServer || muerto.Value || cantidad <= 0f) return;
        vidaActual.Value = Mathf.Min(VidaMaxima, vidaActual.Value + cantidad);
    }

    public void Revivir()
    {
        if (!IsServer) return;
        vidaActual.Value = VidaMaxima;
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