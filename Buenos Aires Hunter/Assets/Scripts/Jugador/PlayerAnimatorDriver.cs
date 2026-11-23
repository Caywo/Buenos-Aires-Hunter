using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// SOLO EL DUEÑO. Calcula los parámetros del Animator a partir del movimiento y las acciones del jugador.
/// ClientNetworkAnimator los replica a los demás jugadores.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimatorDriver : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("Animator del cuerpo (el que ven los demás).")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkAnimator redAnimator;
    [Tooltip("Animator de los brazos en primera persona (solo el dueño los ve). Opcional.")]
    [SerializeField] private Animator animatorBrazos;

    [Header("Armas")]
    [Tooltip("Estilo de animación de cada slot del inventario: 0 = puños, 1 = pistola, 2 = arma larga (escopeta o subfusil).")]
    [SerializeField] private int[] estiloPorSlot = { 1, 0, 2, 2, 0 };

    private CharacterController controller;
    private PlayerMotor motor;
    private PlayerInventory inventario;
    private PlayerCombat combate;
    private MeleeAttack melee;
    private Health salud;

    private Vector3 posicionAnterior;
    private float tiempoEnAire;

    private static readonly int VelocidadHash = Animator.StringToHash("Velocidad");
    private static readonly int SueloHash = Animator.StringToHash("EnSuelo");
    private static readonly int EstiloHash = Animator.StringToHash("Estilo");
    private static readonly int MuertoHash = Animator.StringToHash("Muerto");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        motor = GetComponent<PlayerMotor>();
        inventario = GetComponent<PlayerInventory>();
        combate = GetComponent<PlayerCombat>();
        melee = GetComponent<MeleeAttack>();
        salud = GetComponent<Health>();
    }

    public override void OnNetworkSpawn()
    {
        // Los brazos de primera persona son solo para el dueño.
        if (animatorBrazos != null)
            animatorBrazos.gameObject.SetActive(IsOwner);

        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        posicionAnterior = transform.position;

        if (animatorBrazos != null)
        {
            animatorBrazos.SetBool(SueloHash, true);
            animatorBrazos.SetFloat(VelocidadHash, 0f);
        }

        OcultarCuerpoPropio();

        if (melee != null) melee.OnGolpe += AlGolpear;
        if (combate != null) combate.OnDisparoLocal += AlDisparar;
        if (salud != null) salud.OnMuerteCambiada += AlCambiarMuerte;
    }

    public override void OnNetworkDespawn()
    {
        if (melee != null) melee.OnGolpe -= AlGolpear;
        if (combate != null) combate.OnDisparoLocal -= AlDisparar;
        if (salud != null) salud.OnMuerteCambiada -= AlCambiarMuerte;
    }

    private void Update()
    {
        if (animator == null || motor == null)
            return;

        // Velocidad real a partir del desplazamiento.
        // (CharacterController.velocity no sirve: el motor llama a Move dos veces por frame.)
        Vector3 delta = transform.position - posicionAnterior;
        posicionAnterior = transform.position;

        float horizontal = new Vector3(delta.x, 0f, delta.z).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        animator.SetFloat(VelocidadHash, Mathf.Clamp01(horizontal / Mathf.Max(motor.speed, 0.01f)), 0.1f, Time.deltaTime);

        // Pequeña tolerancia: en rampas y escaleras isGrounded parpadea y haría titilar la animación de salto.
        tiempoEnAire = controller.isGrounded ? 0f : tiempoEnAire + Time.deltaTime;
        animator.SetBool(SueloHash, tiempoEnAire < 0.12f);

        int estilo = EstiloActual();
        animator.SetInteger(EstiloHash, estilo);

        if (animatorBrazos != null)
            animatorBrazos.SetInteger(EstiloHash, estilo);
    }

    private int EstiloActual()
    {
        if (inventario == null)
            return 0;

        int slot = inventario.IndiceActivo;
        return (slot >= 0 && slot < estiloPorSlot.Length) ? estiloPorSlot[slot] : 0;
    }

    private void AlGolpear()
    {
        if (redAnimator != null)
            redAnimator.SetTrigger("Punetazo");

        if (animatorBrazos != null)
            animatorBrazos.SetTrigger("Punetazo");
    }

    private void AlDisparar()
    {
        // Sin arma en mano no hay animación de disparo (el trigger quedaría guardado).
        if (EstiloActual() <= 0)
            return;

        if (redAnimator != null)
            redAnimator.SetTrigger("Disparo");

        if (animatorBrazos != null)
            animatorBrazos.SetTrigger("Disparo");
    }

    private void AlCambiarMuerte(bool muerto)
    {
        if (animator != null)
            animator.SetBool(MuertoHash, muerto);
    }

    // Tu propio cuerpo no se ve (estás dentro de él), pero sigue proyectando sombra.
    private void OcultarCuerpoPropio()
    {
        if (animator == null)
            return;

        foreach (SkinnedMeshRenderer r in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }
}