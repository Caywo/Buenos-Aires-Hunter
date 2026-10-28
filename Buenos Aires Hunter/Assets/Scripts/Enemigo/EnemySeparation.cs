using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// SOLO SERVIDOR. Separa suavemente a los enemigos que se solapan.
/// Complementa la evitación del NavMeshAgent, que no alcanza con enemigos quietos
/// o que van todos hacia el mismo objetivo.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemySeparation : NetworkBehaviour
{
    [Header("Separación")]
    [Tooltip("Radio del cuerpo. Dos enemigos no se acercarán a menos de la suma de sus radios.")]
    [SerializeField] private float radioSeparacion = 0.6f;
    [Tooltip("Los enemigos más pesados se mueven menos al chocar (ej.: Tanque 5, resto 1).")]
    [SerializeField] private float peso = 1f;
    [Tooltip("Velocidad máxima con la que se separan, en metros por segundo.")]
    [SerializeField] private float velocidadSeparacion = 3f;

    private static readonly List<EnemySeparation> todos = new List<EnemySeparation>();

    private NavMeshAgent agent;

    private float RadioSeparacion => radioSeparacion;
    private float Peso => peso;

    // Solo cuentan los enemigos que se mueven con el agente (no los muertos ni los lanzados,
    // ni el Tanque durante la embestida: ahí el agente está desactivado).
    private bool Activo => agent != null && agent.enabled && agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        todos.Add(this);
    }

    private void OnDisable()
    {
        todos.Remove(this);
    }

    private void Update()
    {
        if (!IsServer || !Activo)
            return;

        Vector3 miPos = transform.position;
        Vector3 empuje = Vector3.zero;

        for (int i = 0; i < todos.Count; i++)
        {
            EnemySeparation otro = todos[i];

            if (otro == this || !otro.Activo)
                continue;

            Vector3 delta = miPos - otro.transform.position;
            delta.y = 0f;

            float minimo = radioSeparacion + otro.RadioSeparacion;
            float sq = delta.sqrMagnitude;

            if (sq >= minimo * minimo)
                continue;

            float distancia = Mathf.Sqrt(sq);

            // Si están exactamente en el mismo punto, elegir una dirección estable.
            Vector3 direccion = distancia > 0.001f
                ? delta / distancia
                : transform.right;

            float solape = minimo - distancia;

            // Reparto según el peso: el más liviano cede más.
            float miParte = otro.Peso / (peso + otro.Peso);

            empuje += direccion * solape * miParte;
        }

        if (empuje.sqrMagnitude < 0.0001f)
            return;

        agent.Move(Vector3.ClampMagnitude(empuje, velocidadSeparacion * Time.deltaTime));
    }
}
