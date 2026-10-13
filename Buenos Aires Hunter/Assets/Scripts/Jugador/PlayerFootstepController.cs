using UnityEngine;

public class PlayerFootstepController : MonoBehaviour
{
    private PlayerAudio playerAudio;
    private Vector3 lastPosition;
    private float nextStepTime;
    private float smoothedSpeed;

    [Header("Configuración de Pasos")]
    public float stepInterval = 0.5f;
    public float minSpeedThreshold = 0.5f;
    public float groundCheckDistance = 1.5f;

    void Start()
    {
        playerAudio = GetComponent<PlayerAudio>();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (playerAudio == null) return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float rawSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        // Suaviza la velocidad para evitar parpadeos
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, 10f * Time.deltaTime);

        bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f,
                                        Vector3.down, groundCheckDistance);

        if (grounded && smoothedSpeed > minSpeedThreshold && Time.time >= nextStepTime)
        {
            playerAudio.PlayFootstep();
            nextStepTime = Time.time + stepInterval;
        }
    }
}