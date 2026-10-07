using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public float rango = 1.5f;
    public float radio = 0.5f;
    public float damage = 25f;
    public float fireRate = 0.6f;
    public LayerMask hitMask = ~0;

    private Camera playerCamera;
    private float nextAttackTime = 0f;

    public void Init(Camera playerCamera)
    {
        this.playerCamera = playerCamera;
    }

    public void Atacar()
    {

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + fireRate;

        if (playerCamera == null) return;

        Vector3 origen = playerCamera.transform.position;
        Vector3 direccion = playerCamera.transform.forward;

        if (Physics.SphereCast(origen, radio, direccion, out RaycastHit hit, rango, hitMask))
        {
            Debug.Log("Golpe conectó con: " + hit.collider.name);
        }
    }
}