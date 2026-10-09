using System.IO;
using UnityEngine;
using Unity.Netcode;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string RutaArchivo => Path.Combine(Application.persistentDataPath, "partida.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HayPartidaGuardada()
    {
        return File.Exists(RutaArchivo);
    }

    public SaveData CargarPartida()
    {
        if (!HayPartidaGuardada()) return null;

        string json = File.ReadAllText(RutaArchivo);
        return JsonUtility.FromJson<SaveData>(json);
    }

    // Solo lo llama el servidor/host, al completar el mapa
    public void GuardarPartida(string escenaSiguiente)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var data = new SaveData
        {
            escenaSiguiente = escenaSiguiente,
            jugadores = new JugadorGuardado[2]
        };

        int i = 0;
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            if (i >= 2) break;
            var jugador = kvp.Value.PlayerObject;
            if (jugador == null) { i++; continue; }

            var inventario = jugador.GetComponent<PlayerInventory>();
            data.jugadores[i] = inventario.ObtenerDatosGuardado();
            i++;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(RutaArchivo, json);

        Debug.Log("Partida guardada en: " + RutaArchivo);
    }

    public void BorrarPartida()
    {
        if (File.Exists(RutaArchivo)) File.Delete(RutaArchivo);
    }
}