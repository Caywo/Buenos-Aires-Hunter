
using UnityEngine;
using Unity.Netcode;

public class RangedAttack : NetworkBehaviour
{
    [Header("Disparo")]
    [SerializeField] private GameObject proyectilPrefab;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float distanciaActivacion = 20f;
    [SerializeField] private float tiempoApuntado = 0.7f;
    [SerializeField] private float tiempoEntreDisparos = 2f;

    private float tiempoFinApuntado;
    private float proximoDisparo;
    private bool apuntando;

    public float DistanciaActivacion => distanciaActivacion;

    public void IntentarDisparar(Transform objetivo)
    {
        if (!IsServer || objetivo == null || proyectilPrefab == null)
            return;

        if (Time.time < proximoDisparo)
            return;

        if (!apuntando)
        {
            apuntando = true;
            tiempoFinApuntado = Time.time + tiempoApuntado;
            return;
        }

        if (Time.time < tiempoFinApuntado)
            return;

        Disparar(objetivo);

        apuntando = false;
        proximoDisparo = Time.time + tiempoEntreDisparos;
    }

    public void CancelarApuntado()
    {
        apuntando = false;
    }

    private void Disparar(Transform objetivo)
    {
        Vector3 origen = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + Vector3.up * 1.2f
                + transform.forward * 0.5f;

        Vector3 destino = objetivo.position + Vector3.up;
        Vector3 direccion = (destino - origen).normalized;

        GameObject proyectil = Instantiate(
            proyectilPrefab,
            origen,
            Quaternion.LookRotation(direccion)
        );

        NetworkObject networkObject =
            proyectil.GetComponent<NetworkObject>();

        EnemyProjectile scriptProyectil =
            proyectil.GetComponent<EnemyProjectile>();

        if (networkObject == null || scriptProyectil == null)
        {
            Destroy(proyectil);
            Debug.LogError(
                "El prefab del proyectil necesita NetworkObject " +
                "y EnemyProjectile."
            );
            return;
        }

        networkObject.Spawn();
        scriptProyectil.Inicializar(direccion);
    }
}