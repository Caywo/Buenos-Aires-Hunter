using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LogrosUI : MonoBehaviour
{
    [SerializeField] LogroUI prefabLogro;
    [SerializeField] Transform contenedor;
    [SerializeField] List<LogroData> logros;
    [SerializeField] TMP_Text txtContador;

    readonly Dictionary<string, LogroUI> porId = new Dictionary<string, LogroUI>();

    void Awake() => Construir();

    void Construir()
    {
        if (porId.Count > 0) return;

        foreach (var data in logros)
        {
            if (data == null || porId.ContainsKey(data.id)) continue;
            var ui = Instantiate(prefabLogro, contenedor);
            ui.Configurar(data);
            porId[data.id] = ui;
        }
        ActualizarContador();
    }

    // El dev llama a esto cuando el jugador consigue un logro
    public void Desbloquear(string id) => SetDesbloqueado(id, true);

    public void SetDesbloqueado(string id, bool desbloqueado)
    {
        Construir();
        if (porId.TryGetValue(id, out var ui)) ui.SetDesbloqueado(desbloqueado);
        ActualizarContador();
    }

    // Para cargar todo junto desde la partida guardada
    public void Cargar(IEnumerable<string> idsDesbloqueados)
    {
        Construir();
        foreach (var ui in porId.Values) ui.SetDesbloqueado(false);
        foreach (var id in idsDesbloqueados)
            if (porId.TryGetValue(id, out var ui)) ui.SetDesbloqueado(true);
        ActualizarContador();
    }

    void ActualizarContador()
    {
        if (txtContador == null) return;
        int hechos = 0;
        foreach (var ui in porId.Values) if (ui.Desbloqueado) hechos++;
        txtContador.text = hechos + "/" + porId.Count;
    }
}