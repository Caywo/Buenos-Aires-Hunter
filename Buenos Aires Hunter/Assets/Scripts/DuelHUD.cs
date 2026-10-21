using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// HUD del duelo. Va en el Canvas de la escena "Duelo" y solo LEE el estado de DuelManager.
/// La vida y la munición las sigue mostrando PlayerUI. Los tres textos son opcionales.
/// </summary>
public class DuelHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text textoPuntaje;   // "Vos 1 - 0 Rival"
    [SerializeField] private TMP_Text textoRonda;     // "Ronda 2"
    [SerializeField] private TMP_Text textoCentral;   // cuenta regresiva y mensajes de resultado

    void Update()
    {
        DuelManager dm = DuelManager.Instance;

        if (dm == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Escribir(textoPuntaje, string.Empty);
            Escribir(textoRonda, string.Empty);
            Escribir(textoCentral, string.Empty);
            return;
        }

        ulong yo = NetworkManager.Singleton.LocalClientId;

        dm.ObtenerPuntajes(yo, out int propio, out int rival);
        Escribir(textoPuntaje, $"Vos {propio}  -  {rival} Rival");
        Escribir(textoRonda, dm.Ronda > 0 ? $"Ronda {dm.Ronda}" : string.Empty);
        Escribir(textoCentral, ConstruirMensaje(dm, yo));
    }

    private string ConstruirMensaje(DuelManager dm, ulong yo)
    {
        switch (dm.Estado)
        {
            case EstadoDuelo.Esperando:
                return "Esperando al rival...";

            case EstadoDuelo.CuentaRegresiva:
                return dm.Cuenta > 0 ? dm.Cuenta.ToString() : string.Empty;

            case EstadoDuelo.FinDeRonda:
                return dm.GanadorRonda == (long)yo ? "¡Ganaste la ronda!" : "Perdiste la ronda";

            case EstadoDuelo.FinDePartida:
                return dm.GanadorPartida == (long)yo ? "¡GANASTE EL DUELO!" : "PERDISTE EL DUELO";

            default: // RondaEnCurso
                return string.Empty;
        }
    }

    private void Escribir(TMP_Text texto, string valor)
    {
        if (texto != null) texto.text = valor;
    }
}
