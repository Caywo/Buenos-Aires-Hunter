using UnityEngine;
using Unity.Netcode;

public abstract class Interactuable : NetworkBehaviour
{
    public string mensaje;
    public virtual void Interactuar()
    {

    }

    public virtual void InteractuarConItem(PlayerInventory jugador)
    {

    }
}
