using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Componentes")]
    public AudioSource audioSource;

    [Header("Librería de Sonidos")]
    public AudioClip[] footstepClips; // Array para varios sonidos de pasos
    public AudioClip shootClip;       // Sonido de disparo

    // Método para reproducir un paso al azar
    public void PlayFootstep()
    {
        if (footstepClips.Length == 0 || audioSource == null) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }

    // Método para reproducir el disparo
    public void PlayShoot()
    {
        if (shootClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootClip);
        }
    }
}