using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CinematicManager : MonoBehaviour
{
    [Header("Cinemática")]
    [SerializeField] private PlayableDirector director;

    [Header("Transición")]
    [SerializeField] private string escenaSiguiente;
    private bool transicionIniciada = false;

    private void OnEnable()
    {
        if (director != null)
        {
            director.stopped += AlTerminarCinematica;
        }
    }

    private void Start()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.DetenerMusica();

        if (director == null)
        {
            Debug.LogError("[Cinematica] No se asignó un Playable Director.");
            return;
        }

        if (string.IsNullOrWhiteSpace(escenaSiguiente))
        {
            Debug.LogError("[Cinematica] No se asignó la escena siguiente.");
            return;
        }

        director.Play();
    }
    private void Update()
    {
        if (!TieneAutoridad() || transicionIniciada)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Skip();
        }
    }
    private void OnDisable()
    {
        if (director != null)
        {
            director.stopped -= AlTerminarCinematica;
        }
    }

    private bool TieneAutoridad()
    {
        NetworkManager nm = NetworkManager.Singleton;

        // Sin conexión de red: partida individual.
        if (nm == null || !nm.IsListening)
            return true;

        // En multijugador, solamente el servidor (host).
        return nm.IsServer;
    }

    private void AlTerminarCinematica(PlayableDirector playableDirector)
    {
        if (!TieneAutoridad())
            return;

        CargarSiguienteEscena();
    }

    public void Skip()
    {
        if (transicionIniciada || !TieneAutoridad())
            return;

        // Evitamos que Stop() dispare una transición duplicada.
        if (director != null)
        {
            director.stopped -= AlTerminarCinematica;
            director.Stop();
        }

        CargarSiguienteEscena();
    }

    private void CargarSiguienteEscena()
    {
        if (transicionIniciada)
            return;

        transicionIniciada = true;

        Debug.Log("[Cinematica] Cargando escena: " + escenaSiguiente);

        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null && nm.IsListening)
        {
            if (nm.IsServer)
            {
                nm.SceneManager.LoadScene(
                    escenaSiguiente,
                    LoadSceneMode.Single
                );
            }
        }
        else
        {
            SceneManager.LoadScene(escenaSiguiente);
        }
    }
}