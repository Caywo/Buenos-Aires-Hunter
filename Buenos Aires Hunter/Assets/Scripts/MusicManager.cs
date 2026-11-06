
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Pistas")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField] private AudioClip musicaExtraccion;
    [SerializeField] private AudioClip musicaSupervivencia;
    [SerializeField] private AudioClip musicaDuelo;

    [Header("Nombres de escenas")]
    [SerializeField] private string escenaMenu = "MenuPrincipal";
    [SerializeField] private string escenaExtraccion = "Extraccion";
    [SerializeField] private string escenaSupervivencia = "Supervivencia";
    [SerializeField] private string escenaDuelo = "Duelo";

    private AudioSource fuente;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
    }

    public void ReproducirMusica(AudioClip musica)
    {
        if (musica == null) return;

        if (fuente.clip == musica && fuente.isPlaying)
            return;

        fuente.clip = musica;
        fuente.Play();
    }

    public void ReproducirMenu()
    {
        ReproducirMusica(musicaMenu);
    }

    public void ReproducirExtraccion()
    {
        ReproducirMusica(musicaExtraccion);
    }

    public void ReproducirSupervivencia()
    {
        ReproducirMusica(musicaSupervivencia);
    }

    public void ReproducirDuelo()
    {
        ReproducirMusica(musicaDuelo);
    }

    public void DetenerMusica()
    {
        fuente.Stop();
        fuente.clip = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        ReproducirMenu();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == escenaMenu)
            ReproducirMenu();

        else if (escena.name == escenaExtraccion)
            ReproducirExtraccion();

        else if (escena.name == escenaSupervivencia)
            ReproducirSupervivencia();

        else if (escena.name == escenaDuelo)
            ReproducirDuelo();
    }
}
