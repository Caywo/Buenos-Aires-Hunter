
using UnityEngine;
using UnityEngine.Playables;

public class CinematicaAudio : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    private void OnEnable()
    {
        if (director != null)
            director.played += AlIniciarCinematica;
    }

    private void OnDisable()
    {
        if (director != null)
            director.played -= AlIniciarCinematica;
    }

    private void AlIniciarCinematica(PlayableDirector d)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.DetenerMusica();
        }
    }
}
