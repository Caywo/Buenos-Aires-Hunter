using UnityEngine;
using TMPro;
using Unity.Netcode;

public class HudScript : MonoBehaviour
{
    public TMP_Text textoSaldo;

    private PlayerInventory inventarioLocal;

    void Update()
    {
        Debug.Log("HudScript vivo, frame: " + Time.frameCount);
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
            Debug.Log($"mostrar={mostrar}");
            textoSaldo.gameObject.SetActive(mostrar);

            if (mostrar)
            {
                textoSaldo.text = $"$ {inventarioLocal.saldoSube.Value}";
            }
        }
    }
}