using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Componentes")]
    public AudioSource audioSource;

    [Header("Sonidos de pasos")]
    public AudioClip[] footstepClips;

    [Header("Sonido de puñetazo")]
    public AudioClip punchClip;

    [Header("Configuración de Audio")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.3f;

    [Range(0f, 1f)]
    public float punchVolume = 0.5f;

    // Reproduce un paso al azar
    public void PlayFootstep()
    {
        if (footstepClips.Length == 0 || audioSource == null) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        audioSource.PlayOneShot(clip, footstepVolume);
    }

    // Reproduce el sonido del puñetazo
    public void PlayPunch()
    {
        if (punchClip == null || audioSource == null) return;

        audioSource.PlayOneShot(punchClip, punchVolume);
    }
}