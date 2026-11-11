using Unity.Netcode;
using UnityEngine;

// Va en el prefab de cada enemigo (normal, tanque y boss), junto a su AudioSource.
// - Pasos: se calculan en CADA máquina según cuánto se movió el enemigo (no dependen de la IA).
// - Ataque, muerte y sonidos extra: los dispara el servidor y llegan a todos los clientes por ClientRpc.
// Los sonidos que no se usan se dejan vacíos y simplemente no suenan.
[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : NetworkBehaviour
{
    [Header("Pasos")]
    [SerializeField] AudioClip[] pasos;
    [Tooltip("Metros que recorre entre un paso y el siguiente. Más grande = pasos más espaciados.")]
    [SerializeField] float distanciaPorPaso = 1.6f;
    [SerializeField, Range(0f, 1f)] float volumenPasos = 0.5f;
    [Tooltip("Si se mueve más rápido que esto (m/s) no suenan pasos: sirve para dashes y embestidas. 0 = sin límite.")]
    [SerializeField] float velocidadMaximaPasos = 0f;

    [Header("Ataque")]
    [SerializeField] AudioClip[] ataques;
    [SerializeField, Range(0f, 1f)] float volumenAtaque = 0.8f;

    [Header("Muerte")]
    [SerializeField] AudioClip[] muertes;
    [SerializeField, Range(0f, 1f)] float volumenMuerte = 0.9f;

    [Header("Extra para jefes (opcional)")]
    [Tooltip("Aviso antes del ataque especial: le da al jugador una pista para esquivar.")]
    [SerializeField] AudioClip[] preparacionEspecial;
    [SerializeField] AudioClip[] dash;
    [Tooltip("Rugido al pasar a la fase 2.")]
    [SerializeField] AudioClip[] fase2;
    [SerializeField, Range(0f, 1f)] float volumenExtra = 1f;

    [Header("Variación")]
    [SerializeField] Vector2 variacionPitch = new Vector2(0.92f, 1.08f);

    [Header("Optimización")]
    [Tooltip("Si la cámara está más lejos que esto, no se reproducen pasos.")]
    [SerializeField] float distanciaMaximaPasos = 30f;

    enum Extra : byte { PreparacionEspecial = 0, Dash = 1, Fase2 = 2 }

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

        // Movimiento demasiado rápido (dash, embestida, knockback): no es una pisada
        if (velocidadMaximaPasos > 0f && avance / Mathf.Max(Time.deltaTime, 0.0001f) > velocidadMaximaPasos)
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

    // ───────────────────────── SONIDOS QUE PIDE EL SERVIDOR ─────────────────────────

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

    // Jefes: aviso antes del ataque especial
    public void ReproducirPreparacionEspecial()
    {
        if (!IsServer || muerto) return;
        ExtraClientRpc((byte)Extra.PreparacionEspecial);
    }

    // Jefes: al lanzarse en el dash
    public void ReproducirDash()
    {
        if (!IsServer || muerto) return;
        ExtraClientRpc((byte)Extra.Dash);
    }

    // Jefes: rugido al empezar la transición a fase 2
    public void ReproducirFase2()
    {
        if (!IsServer || muerto) return;
        ExtraClientRpc((byte)Extra.Fase2);
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

    [ClientRpc]
    void ExtraClientRpc(byte tipo)
    {
        if (muerto) return;

        AudioClip[] lista = null;
        switch ((Extra)tipo)
        {
            case Extra.PreparacionEspecial: lista = preparacionEspecial; break;
            case Extra.Dash: lista = dash; break;
            case Extra.Fase2: lista = fase2; break;
        }

        Reproducir(Elegir(lista), volumenExtra);
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