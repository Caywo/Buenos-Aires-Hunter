using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class EnemyProjectile : NetworkBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private float velocidad = 12f;
    [SerializeField] private float dano = 15f;
    [SerializeField] private float tiempoMaximoVida = 4f;

    private Vector3 direccion;
    private float tiempoSpawn;
    private bool impacto;

    public void Inicializar(Vector3 nuevaDireccion)
    {
        Debug.Log("[BOLA] Inicializar llamado. IsServer = " + IsServer);

        if (!IsServer) return;

        direccion = nuevaDireccion.normalized;
        tiempoSpawn = Time.time;

        Debug.Log("[BOLA] Inicializada en el servidor. Dirección: " + direccion);
    }

    private void Update()
    {
        if (Time.frameCount % 120 == 0)
            Debug.Log("[BOLA] Update ejecutándose. IsServer = " + IsServer);

        if (!IsServer || impacto) return;

        transform.position += direccion * velocidad * Time.deltaTime;

        if (Time.time >= tiempoSpawn + tiempoMaximoVida)
            Desaparecer();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!IsServer || impacto) return;

        Debug.Log("[EnemyProjectile] Choque con: " + otro.name);

        // Ignorar otros proyectiles.
        if (otro.GetComponentInParent<EnemyProjectile>() != null)
            return;

        Health vida = otro.GetComponentInParent<Health>();

        if (vida != null)
        {
            Debug.Log(
                "[EnemyProjectile] Health encontrado. EsJugador: "
                + vida.EsJugador
                + ", EstaMuerto: "
                + vida.EstaMuerto
            );

            // No dañar enemigos.
            if (!vida.EsJugador)
            {
                Debug.Log("[EnemyProjectile] El objetivo no es un jugador.");
                return;
            }

            if (vida.EstaMuerto)
                return;

            impacto = true;

            Debug.Log("[EnemyProjectile] Aplicando " + dano + " de daño.");

            vida.TakeDamage(dano, ReglasCombate.AtacanteNeutral);
            Desaparecer();
            return;
        }

        // Desaparecer al tocar paredes u otros objetos sólidos.
        impacto = true;
        Desaparecer();
    }

    private void Desaparecer()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}