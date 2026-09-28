using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerUI : MonoBehaviour
{
    public TMP_Text textoSaldo;
    public TMP_Text textoInteraccion;
    private PlayerInventory inventarioLocal;

    void Update()
    {
        if (inventarioLocal == null)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
            if (NetworkManager.Singleton.LocalClient == null) return;

            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObject == null) return;

            inventarioLocal = playerObject.GetComponent<PlayerInventory>();
        }
        else
        {
            bool mostrar = inventarioLocal.TieneSubeEquipada;
            textoSaldo.gameObject.SetActive(mostrar);

            if (mostrar)
            {
                textoSaldo.text = $"$ {inventarioLocal.saldoSube.Value}";
            }
        }
    }
    
    public void ActualizarTexto(string mensajeInteraccion)
    {
        textoInteraccion.text = mensajeInteraccion;
    }
}