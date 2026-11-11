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
    [SerializeField] private AudioClip musicaTutorial;

    [Header("Nombres de escenas")]
    [SerializeField] private string escenaMenu = "MenuPrincipal";
    [SerializeField] private string escenaExtraccion = "Extraccion";
    [SerializeField] private string escenaSupervivencia = "Supervivencia";
    [SerializeField] private string escenaDuelo = "Duelo";
    [SerializeField] private string escenaTutorial = "Tutorial";

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
        // Re-aplica la configuración de audio al AudioMixer antes de iniciar la música
        if (Configuracion.Instance != null)
        {
            Configuracion.Instance.AplicarTodoElAudio();
        }

        ReproducirMenu();
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

    public void ReproducirTutorial()
    {
        ReproducirMusica(musicaTutorial);
    }

    public void DetenerMusica()
    {
        fuente.Stop();
        fuente.clip = null;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Al cambiar de escena se asegura de mantener aplicados los niveles del Mixer
        if (Configuracion.Instance != null)
        {
            Configuracion.Instance.AplicarTodoElAudio();
        }

        if (escena.name == escenaMenu)
            ReproducirMenu();
        else if (escena.name == escenaExtraccion)
            ReproducirExtraccion();
        else if (escena.name == escenaSupervivencia)
            ReproducirSupervivencia();
        else if (escena.name == escenaDuelo)
            ReproducirDuelo();
        else if (escena.name == escenaTutorial)
            ReproducirTutorial();
    }
}