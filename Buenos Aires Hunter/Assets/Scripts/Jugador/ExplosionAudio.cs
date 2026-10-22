using UnityEngine;

public class ExplosionAudio : MonoBehaviour
{
    [Header("Sonido")]
    public AudioClip explosionClip;

    [Range(0f, 1f)]
    public float volume = 0.5f;

    private void Start()
    {
        if (explosionClip != null)
        {
            AudioSource.PlayClipAtPoint(
                explosionClip,
                transform.position,
                volume
            );
        }
    }
}