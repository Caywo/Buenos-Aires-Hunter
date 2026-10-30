using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Registro de logros del perfil de ESTA computadora (PlayerPrefs). Cada jugador tiene los suyos.
///
/// - Guarda qué logros están desbloqueados y los contadores de progreso (bajas, gasto en la tienda...).
/// - Sobrevive entre escenas. Tiene que estar en un objeto RAÍZ de la escena MenuPrincipal
///   (DontDestroyOnLoad no funciona en objetos hijos).
/// - Le pasa el estado a LogrosUI cada vez que carga una escena y cada vez que se desbloquea algo,
///   así la pantalla de logros siempre se ve actualizada.
/// - Desbloquear con: LogrosManager.Instance.Desbloquear("ID_DEL_LOGRO");
/// </summary>
public class LogrosManager : MonoBehaviour
{
    public static LogrosManager Instance { get; private set; }

    /// <summary>Se dispara solo cuando se desbloquea un logro NUEVO. Sirve para el aviso en pantalla.</summary>
    public event Action<string> AlDesbloquear;

    private const string ClaveLogros = "logros_desbloqueados";
    private const string ClaveContadores = "logros_contadores";

    private readonly HashSet<string> desbloqueados = new HashSet<string>();
    private readonly Dictionary<string, int> contadores = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // al volver al menú, la copia nueva de la escena se descarta
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Cargar();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void Start()
    {
        ActualizarUI();
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        Guardar();
        Instance = null;
    }

    void OnApplicationQuit()
    {
        Guardar();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Guardar();      // aprovecha cada cambio de escena para guardar los contadores
        ActualizarUI(); // si la escena nueva tiene la pantalla de logros, la deja al día
    }

    // ───────────────────────── Logros ─────────────────────────

    public bool EstaDesbloqueado(string id) => desbloqueados.Contains(id);

    public int CantidadDesbloqueados => desbloqueados.Count;

    public void Desbloquear(string id)
    {
        if (string.IsNullOrEmpty(id) || !desbloqueados.Add(id)) return; // vacío, o ya lo tenía

        Guardar();
        AlDesbloquear?.Invoke(id);

        LogrosUI ui = BuscarUI();
        if (ui != null) ui.Desbloquear(id);
    }

    // ───────────────────────── Progreso ─────────────────────────

    /// <summary>Suma al contador y devuelve el total. Se guarda al desbloquear algo o al cambiar de escena.</summary>
    public int Sumar(string contador, int cantidad)
    {
        contadores.TryGetValue(contador, out int actual);
        actual += cantidad;
        contadores[contador] = actual;
        return actual;
    }

    public int ObtenerContador(string contador)
    {
        return contadores.TryGetValue(contador, out int valor) ? valor : 0;
    }

    // ───────────────────────── Guardado ─────────────────────────

    public void Guardar()
    {
        PlayerPrefs.SetString(ClaveLogros, string.Join(";", desbloqueados));

        var partes = new List<string>();
        foreach (var par in contadores) partes.Add(par.Key + "=" + par.Value);
        PlayerPrefs.SetString(ClaveContadores, string.Join(";", partes));

        PlayerPrefs.Save();
    }

    private void Cargar()
    {
        desbloqueados.Clear();
        contadores.Clear();

        foreach (string id in PlayerPrefs.GetString(ClaveLogros, "").Split(';'))
        {
            if (!string.IsNullOrEmpty(id)) desbloqueados.Add(id);
        }

        foreach (string par in PlayerPrefs.GetString(ClaveContadores, "").Split(';'))
        {
            string[] partes = par.Split('=');
            if (partes.Length == 2 && int.TryParse(partes[1], out int valor)) contadores[partes[0]] = valor;
        }
    }

    /// <summary>Para pruebas: clic derecho en el componente > "Borrar todos los logros y contadores".</summary>
    [ContextMenu("Borrar todos los logros y contadores")]
    public void BorrarTodo()
    {
        desbloqueados.Clear();
        contadores.Clear();
        PlayerPrefs.DeleteKey(ClaveLogros);
        PlayerPrefs.DeleteKey(ClaveContadores);
        PlayerPrefs.Save();
        ActualizarUI();
    }

    // ───────────────────────── Pantalla de logros ─────────────────────────

    // LogrosUI vive en la escena del menú, normalmente en un panel desactivado: hay que incluir los inactivos
    private LogrosUI BuscarUI()
    {
        return FindAnyObjectByType<LogrosUI>(FindObjectsInactive.Include);
    }

    private void ActualizarUI()
    {
        LogrosUI ui = BuscarUI();
        if (ui != null) ui.Cargar(desbloqueados);
    }
}
