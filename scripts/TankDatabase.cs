using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TankDatabase", menuName = "Tanques/Tank Database")]
public class TankDatabase : ScriptableObject
{
    [Tooltip("Arrastra aquí TODOS tus assets de TankData (RhinoTank, WaspTank, etc.)")]
    public List<TankData> allTanks;
    
    //Busca un tanque en la base de datos por su Tank ID (ej. "Rhino").
    public TankData GetTankByID(string id)
    {
        foreach (TankData tank in allTanks)
        {
            if (tank.tankID == id)
            {
                return tank;
            }
        }
        Debug.LogWarning("¡No se encontró un TankData con el ID: " + id);
        return null;
    }
}