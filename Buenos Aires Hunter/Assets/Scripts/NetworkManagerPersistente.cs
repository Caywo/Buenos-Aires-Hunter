using UnityEngine;

public class NetworkManagerPersistente : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}