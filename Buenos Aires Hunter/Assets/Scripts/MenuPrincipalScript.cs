using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void UnJugador() 
    { 
        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData("127.0.0.1", 7777);

        NetworkManager.Singleton.StartHost();


        NetworkManager.Singleton.SceneManager.LoadScene(
            "Escenario1",
            LoadSceneMode.Single
        );
    }
}