using System.Collections.Generic;
using UnityEngine;

public class TiendaUI : MonoBehaviour
{
    public GameObject panelTienda;
    private PlayerInventory jugadorActual;
    private MinijuegoPPTUI minijuegoUI;

    // Objetos de la tienda que se ocultaron mientras se juega el mini juego.
    private readonly List<GameObject> ocultosPorMinijuego = new List<GameObject>();

    public bool EstaAbierta => panelTienda.activeSelf;

    public void AbrirTienda(PlayerInventory jugador)
    {
        jugadorActual = jugador;

        // Si volvemos del mini juego, se muestra de nuevo lo que se había ocultado.
        MostrarContenidoOcultado();

        panelTienda.SetActive(true);

        jugador.GetComponent<PlayerLook>().inputBloqueado = true;
        jugador.inputBloqueado = true;
        jugador.GetComponent<PlayerMotor>().inputBloqueado = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        MostrarContenidoOcultado();
        panelTienda.SetActive(false);

        if (jugadorActual != null)
        {
            jugadorActual.GetComponent<PlayerLook>().inputBloqueado = false;
            jugadorActual.inputBloqueado = false;
            jugadorActual.GetComponent<PlayerMotor>().inputBloqueado = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Lo llama el botón "De una": oculta la tienda y abre el mini juego.</summary>
    public void IniciarMinijuego()
    {
        Debug.Log("[Tienda] Botón 'De una' apretado.");

        if (jugadorActual == null)
        {
            Debug.LogWarning("[Tienda] jugadorActual es null: la tienda no se abrió con AbrirTienda().");
            return;
        }

        MinijuegoPiedraPapelTijera juego = jugadorActual.GetComponent<MinijuegoPiedraPapelTijera>();

        if (juego == null)
        {
            Debug.LogWarning("[Tienda] El jugador no tiene el componente MinijuegoPiedraPapelTijera (falta agregarlo al prefab del jugador).");
            return;
        }

        if (minijuegoUI == null)
            minijuegoUI = FindAnyObjectByType<MinijuegoPPTUI>(FindObjectsInactive.Include);

        if (minijuegoUI == null)
        {
            Debug.LogWarning("[Tienda] No se encontró el objeto con MinijuegoPPTUI en la escena.");
            return;
        }

        PlayerInventory jugador = jugadorActual;

        if (minijuegoUI.transform.IsChildOf(panelTienda.transform))
        {
            // El mini juego está dentro de la tienda: la tienda sigue activa y solo se oculta su contenido.
            OcultarContenidoTienda(minijuegoUI.transform);
        }
        else
        {
            CerrarTienda();
        }

        minijuegoUI.Abrir(juego, jugador, this);

        if (!minijuegoUI.gameObject.activeInHierarchy)
        {
            Transform t = minijuegoUI.transform.parent;
            while (t != null && t.gameObject.activeSelf) t = t.parent;
            Debug.LogError($"[Tienda] MinijuegoPPT sigue apagado porque '{(t != null ? t.name : "?")}' está desactivado.");
        }
    }

    // Oculta todo lo que hay dentro de la tienda, menos el camino que lleva hasta el mini juego.
    private void OcultarContenidoTienda(Transform minijuego)
    {
        MostrarContenidoOcultado();

        Transform actual = minijuego;

        while (actual != null && actual != panelTienda.transform)
        {
            Transform padre = actual.parent;

            if (padre == null)
                break;

            foreach (Transform hermano in padre)
            {
                if (hermano == actual || !hermano.gameObject.activeSelf)
                    continue;

                hermano.gameObject.SetActive(false);
                ocultosPorMinijuego.Add(hermano.gameObject);
            }

            actual = padre;
        }
    }

    private void MostrarContenidoOcultado()
    {
        foreach (GameObject go in ocultosPorMinijuego)
        {
            if (go != null)
                go.SetActive(true);
        }

        ocultosPorMinijuego.Clear();
    }

    public void Comprar(int precio, int indice)
    {
        if (jugadorActual == null) return;
        jugadorActual.IntentarComprarRpc(precio, indice);
    }

    public void ComprarMejoraVelocidad(int precio)
    {
        if (jugadorActual == null) return;
        var mejoras = jugadorActual.GetComponent<PlayerMejoras>();
        mejoras?.IntentarComprarVelocidadRpc(precio);
    }
}