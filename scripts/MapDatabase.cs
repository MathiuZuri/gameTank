using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Tanques/Map Database")]
public class MapDatabase : ScriptableObject
{
    public List<MapData> allMaps;

    public MapData GetMapByID(string id)
    {
        foreach (MapData map in allMaps)
        {
            if (map.mapID == id) 
            {
                return map;
            }
        }
        Debug.LogWarning("No se encontró un mapa con el ID: " + id);
        return allMaps[0]; // Devuelve el primero por defecto
    }
}