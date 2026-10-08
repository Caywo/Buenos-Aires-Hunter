using System;
using UnityEngine;

/// <summary>
/// Ataque cuerpo a cuerpo (puños). Guarda las stats del golpe (las lee el servidor) y aplica el
/// cooldown local. El golpe real lo valida y lo aplica el servidor en PlayerCombat.
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    [Header("Golpe (los valida el servidor)")]
    [Tooltip("Alcance del golpe medido desde la cámara.")]
    public float rango = 10f;
    [Tooltip("Grosor del golpe: más grande = más fácil de acertar.")]
    public float radio = 1f;
    public float damage = 25f;
    [Tooltip("Segundos entre golpes.")]
    public float fireRate = 0.6f;
    public LayerMask hitMask = ~0;

    private Camera playerCamera;
    private PlayerCombat combate;
    private float nextAttackTime = 0f;

    /// <summary>Se dispara en el dueño al lanzar un golpe. Ideal para enganchar animación y sonido.</summary>
    public event Action OnGolpe;

    void Awake()
    {
        combate = GetComponent<PlayerCombat>();
    }

    public void Init(Camera playerCamera)
    {
        this.playerCamera = playerCamera;
    }

    /// <summary>Lo llama PlayerInventory (solo dueño) cuando hacés click con las manos vacías.</summary>
    public void Atacar()
    {
        if (Time.time < nextAttackTime) return;
        if (playerCamera == null || combate == null)
        {
            Debug.LogWarning($"[Melee] No se puede atacar. Camara null: {playerCamera == null}, PlayerCombat null: {combate == null}");
            return;
        }

        nextAttackTime = Time.time + fireRate;

        Debug.Log("[Melee] Golpe lanzado en el cliente");
        OnGolpe?.Invoke();

        combate.IntentarMelee(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );
    }
}