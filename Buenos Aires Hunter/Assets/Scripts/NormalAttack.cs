using UnityEngine;
using Unity.Netcode;

public class NormalAttack : NetworkBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float danoAtaque = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    private float tiempoUltimoAtaque = -Mathf.Infinity;

    public bool PuedeAtacar()
    {
        if (!IsServer)
            return false;

        return Time.time >= tiempoUltimoAtaque + attackCooldown;
    }

    public void Atacar(Transform objetivo)
    {
        if (!IsServer)
            return;

        if (objetivo == null)
            return;

        if (!PuedeAtacar())
            return;

        tiempoUltimoAtaque = Time.time;

        IDamageable damageable =
            objetivo.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(
            danoAtaque,
            ReglasCombate.AtacanteNeutral
        );
    }
}
