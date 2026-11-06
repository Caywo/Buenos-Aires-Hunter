using Unity.Netcode;
using UnityEngine;

// Va en el prefab de cada enemigo (normal y tanque), junto a su AudioSource.
// - Pasos: se calculan en CADA máquina según cuánto se movió el enemigo (no dependen de la IA).
// - Ataque y muerte: los dispara el servidor y llegan a todos los clientes por ClientRpc.
[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : NetworkBehaviour
{
    [Header("Pasos")]
    [SerializeField] AudioClip[] pasos;
    [Tooltip("Metros que recorre entre un paso y el siguiente. Más grande = pasos más espaciados.")]
    [SerializeField] float distanciaPorPaso = 1.6f;
    [SerializeField, Range(0f, 1f)] float volumenPasos = 0.5f;

    [Header("Ataque")]
    [SerializeField] AudioClip[] ataques;
    [SerializeField, Range(0f, 1f)] float volumenAtaque = 0.8f;

    [Header("Muerte")]
    [SerializeField] AudioClip[] muertes;
    [SerializeField, Range(0f, 1f)] float volumenMuerte = 0.9f;

    [Header("Variación")]
    [SerializeField] Vector2 variacionPitch = new Vector2(0.92f, 1.08f);

    [Header("Optimización")]
    [Tooltip("Si la cámara está más lejos que esto, no se reproducen pasos.")]
    [SerializeField] float distanciaMaximaPasos = 30f;

    AudioSource fuente;
    Vector3 ultimaPosicion;
    float distanciaAcumulada;
    bool muerto;

    void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 1f;   // sonido 3D: se oye desde donde está el enemigo

        // Que el volumen del menú de opciones afecte a los enemigos
        if (fuente.outputAudioMixerGroup == null && Configuracion.Instance != null)
            fuente.outputAudioMixerGroup = Configuracion.Instance.grupoSonidos;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        muerto = false;
        distanciaAcumulada = 0f;
        ultimaPosicion = transform.position;
    }

    // ───────────────────────── PASOS (todas las máquinas) ─────────────────────────

    void Update()
    {
        if (muerto) return;

        Vector3 delta = transform.position - ultimaPosicion;
        delta.y = 0f;
        ultimaPosicion = transform.position;

        float avance = delta.magnitude;

        // Un salto enorme en un frame es un teletransporte o un respawn, no una caminata
        if (avance > 2f)
        {
            distanciaAcumulada = 0f;
            return;
        }

        distanciaAcumulada += avance;

        if (distanciaAcumulada >= distanciaPorPaso)
        {
            distanciaAcumulada -= distanciaPorPaso;
            ReproducirPaso();
        }
    }

    void ReproducirPaso()
    {
        if (!EstaCerca()) return;
        Reproducir(Elegir(pasos), volumenPasos);
    }

    bool EstaCerca()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;

        return (cam.transform.position - transform.position).sqrMagnitude
               <= distanciaMaximaPasos * distanciaMaximaPasos;
    }

    // ───────────────────────── ATAQUE Y MUERTE (los pide el servidor) ─────────────────────────

    // Llamar desde el servidor, en el momento exacto en que el enemigo ataca
    public void ReproducirAtaque()
    {
        if (!IsServer || muerto) return;
        AtaqueClientRpc();
    }

    // Llamar desde el servidor, cuando el enemigo muere
    public void ReproducirMuerte()
    {
        if (!IsServer || muerto) return;
        MuerteClientRpc();
    }

    [ClientRpc]
    void AtaqueClientRpc()
    {
        if (muerto) return;
        Reproducir(Elegir(ataques), volumenAtaque);
    }

    [ClientRpc]
    void MuerteClientRpc()
    {
        muerto = true;
        ReproducirDesligado(Elegir(muertes), volumenMuerte);
    }

    // ───────────────────────── Utilidades ─────────────────────────

    static AudioClip Elegir(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    void Reproducir(AudioClip clip, float volumen)
    {
        if (clip == null) return;

        fuente.pitch = Random.Range(variacionPitch.x, variacionPitch.y);
        fuente.PlayOneShot(clip, volumen);
    }

    // La muerte usa un objeto temporal: si el enemigo se destruye justo después,
    // el sonido no se corta.
    void ReproducirDesligado(AudioClip clip, float volumen)
    {
        if (clip == null) return;

        var go = new GameObject("SonidoMuerteEnemigo");
        go.transform.position = transform.position;

        var temp = go.AddComponent<AudioSource>();
        temp.clip = clip;
        temp.volume = volumen;
        temp.pitch = Random.Range(variacionPitch.x, variacionPitch.y);
        temp.spatialBlend = 1f;
        temp.minDistance = fuente.minDistance;
        temp.maxDistance = fuente.maxDistance;
        temp.rolloffMode = fuente.rolloffMode;
        temp.outputAudioMixerGroup = fuente.outputAudioMixerGroup;
        temp.Play();

        Destroy(go, clip.length / Mathf.Max(0.1f, Mathf.Abs(temp.pitch)) + 0.1f);
    }
}
