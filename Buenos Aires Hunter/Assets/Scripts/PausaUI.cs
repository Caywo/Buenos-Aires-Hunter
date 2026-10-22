using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class PausaUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelPausa;

    [Header("Escena")]
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";

    private PlayerInventory jugador;
    private PlayerLook mirar;
    private PlayerMotor motor;

    private bool pausaAbierta;
    private bool volviendoAlMenu;

    public bool PausaAbierta => pausaAbierta;

    private void Awake()
    {
        if (panelPausa != null)
            panelPausa.SetActive(false);
    }

    private void Start()
    {
        BuscarJugadorLocal();
        ActualizarCursor(false);
    }

    private void BuscarJugadorLocal()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsListening)
            return;

        NetworkObject jugadorLocal =
            nm.SpawnManager.GetLocalPlayerObject();

        if (jugadorLocal == null)
            return;

        jugador = jugadorLocal.GetComponent<PlayerInventory>();
        mirar = jugadorLocal.GetComponent<PlayerLook>();
        motor = jugadorLocal.GetComponent<PlayerMotor>();
    }

    public void AbrirMenuDePausa()
    {
        if (pausaAbierta || volviendoAlMenu)
            return;

        if (jugador == null)
            BuscarJugadorLocal();

        pausaAbierta = true;

        if (panelPausa != null)
            panelPausa.SetActive(true);
        else
            Debug.LogError("PausaUI: no asignaste Panel Pausa en el Inspector.");

        BloquearControles(true);
        ActualizarCursor(true);
    }

    public void CerrarMenuDePausa()
    {
        if (!pausaAbierta || volviendoAlMenu)
            return;

        pausaAbierta = false;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        BloquearControles(false);
        ActualizarCursor(false);
    }

    private void BloquearControles(bool bloquear)
    {
        if (jugador != null)
            jugador.inputBloqueado = bloquear;

        if (mirar != null)
            mirar.inputBloqueado = bloquear;

        if (motor != null)
            motor.inputBloqueado = bloquear;
    }

    private void ActualizarCursor(bool visible)
    {
        Cursor.lockState = visible
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = visible;
    }

    public void VolverAlMenu()
    {
        if (volviendoAlMenu)
            return;

        volviendoAlMenu = true;
        pausaAbierta = false;

        BloquearControles(true);

        if (panelPausa != null)
            panelPausa.SetActive(false);

        ActualizarCursor(true);

        StartCoroutine(DesconectarYVolver());
    }

    private IEnumerator DesconectarYVolver()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null && nm.IsListening)
        {
            nm.Shutdown();

            float tiempoEspera = 0f;

            while (nm != null && nm.IsListening && tiempoEspera < 5f)
            {
                tiempoEspera += Time.unscaledDeltaTime;
                yield return null;
            }

            // Dejar que Netcode procese el apagado.
            yield return null;
        }

        SceneManager.LoadScene(escenaMenuPrincipal);
    }
}