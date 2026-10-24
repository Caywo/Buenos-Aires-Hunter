using UnityEngine;

[CreateAssetMenu(menuName = "Logros/Logro")]
public class LogroData : ScriptableObject
{
    public string id;                 // único, no cambiarlo una vez en uso
    public string titulo;
    [TextArea] public string descripcion;
    public bool secreto;              // muestra "???" hasta desbloquearlo
}