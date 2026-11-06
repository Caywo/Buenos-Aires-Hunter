using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class EnemyProjectile : NetworkBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private float velocidad = 12f;
    [SerializeField] private float dano = 15f;
    [SerializeField] private float tiempoMaximoVida = 4f;

    [Header("Detección de impacto")]
    [Tooltip("Capas que detienen la bola: Jugador y el escenario (Default). NO incluir Enemigo.")]
    [SerializeField] private LayerMask capasImpacto = ~0;
    [SerializeField] private float radioImpacto = 0.3f;

    private readonly Collider[] buffer = new Collider[8];

    private Vector3 direccion;
    private float tiempoSpawn;
    private bool inicializado;
    private bool impacto;

    public void Inicializar(Vector3 nuevaDireccion)
    {
        if (!IsServer) return;

        direccion = nuevaDireccion.normalized;
        tiempoSpawn = Time.time;
        inicializado = true;
    }

    private void Update()
    {
        if (!IsServer || impacto || !inicializado) return;

        if (Time.time >= tiempoSpawn + tiempoMaximoVida)
        {
            Desaparecer();
            return;
        }

        Vector3 inicio = transform.position;
        float paso = velocidad * Time.deltaTime;

        // 1) ¿Ya está tocando a un jugador? (por ejemplo, un disparo a quemarropa)
        int cantidad = Physics.OverlapSphereNonAlloc(
            inicio, radioImpacto, buffer, capasImpacto, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < cantidad; i++)
        {
            if (ProcesarImpacto(buffer[i]))
                return;
        }

        // 2) Barrido hacia adelante, para que no atraviese nada a alta velocidad.
        if (Physics.SphereCast(
            inicio, radioImpacto, direccion, out RaycastHit hit, paso,
            capasImpacto, QueryTriggerInteraction.Ignore))
        {
            if (ProcesarImpacto(hit.collider))
                return;
        }

        transform.position = inicio + direccion * paso;
    }

    /// <summary>Devuelve true si la bola impactó y desapareció.</summary>
    private bool ProcesarImpacto(Collider otro)
    {
        // Ignorar otros proyectiles.
        if (otro.GetComponentInParent<EnemyProjectile>() != null)
            return false;

        Health vida = otro.GetComponentInParent<Health>();

        if (vida != null)
        {
            // Los enemigos no bloquean ni reciben daño de la bola.
            if (!vida.EsJugador)
                return false;

            if (vida.EstaMuerto)
                return false;

            impacto = true;
            Debug.Log($"[EnemyProjectile] {dano} de daño a {vida.name}");
            vida.TakeDamage(dano, ReglasCombate.AtacanteNeutral);
            Desaparecer();
            return true;
        }

        // Paredes, suelo u otros objetos sólidos.
        impacto = true;
        Desaparecer();
        return true;
    }

    private void Desaparecer()
    {
        if (!IsServer) return;

        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}