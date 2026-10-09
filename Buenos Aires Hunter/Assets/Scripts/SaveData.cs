using System;

[Serializable]
public class ItemGuardado
{
    public int indiceSlot;
    public int cantidad;
}

[Serializable]
public class JugadorGuardado
{
    public int saldoSube;
    public ItemGuardado[] items;
}

[Serializable]
public class SaveData
{
    public string escenaSiguiente;
    public JugadorGuardado[] jugadores; // índice 0 y 1, igual que tu PlayerSpawnManager
}