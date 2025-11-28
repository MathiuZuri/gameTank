using UnityEngine;
using System.Collections.Generic; // Para usar Listas

// Esto permite crear mapas desde el menú de assets
[CreateAssetMenu(fileName = "NewMap", menuName = "Tanques/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Identificación y UI")]
    [Tooltip("El ID único para este mapa (ej: 'Maze_01')")]
    public string mapID; 
    [Tooltip("El nombre que se muestra en el menú (ej: 'Laberinto de Fuego')")]
    public string mapName;
    [Tooltip("La imagen de previsualización para el menú de selección.")]
    public Sprite previewImage;
    [Tooltip("Descripción para el menú de selección.")]
    [TextArea(3, 5)] 
    public string description;

    [Header("Matriz del Mapa (filas de arriba a abajo)")]
    [Tooltip("0=Vacío, 1=Muro, 2=Cobertura, 8=Spawn Jugador, 9=Spawn Enemigo")]
    
    // No podemos guardar int[,] en el inspector, así que usamos un truco:
    // una Lista de strings, donde cada string es una fila.
    public List<string> mapRows = new List<string>();
    //Convierte la Lista de strings en la matriz int[,] que usa el generador.
    public int[,] GetMapMatrix()
    {
        if (mapRows.Count == 0 || mapRows[0].Length == 0)
        {
            Debug.LogError("¡La matriz del mapa '" + mapName + "' está vacía!");
            return new int[0, 0];
        }

        int height = mapRows.Count; // Filas (Y)
        int width = mapRows[0].Length; // Columnas (X)

        int[,] matrix = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            if (mapRows[y].Length != width)
            {
                Debug.LogError("Error en Mapa '" + mapName + "': La fila " + y + " tiene un tamaño diferente. Se esperaba " + width);
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                int tileValue = (int)char.GetNumericValue(mapRows[y][x]);

                if (tileValue >= 0) // char.GetNumericValue devuelve -1 si no es un número
                {
                    matrix[y, x] = tileValue;
                }
                else
                {
                    Debug.LogWarning("Caracter no válido '" + mapRows[y][x] + "' en el mapa '" + mapName + "', se usará 0 en (" + x + "," + y + ").");
                    matrix[y, x] = 0; // Si es 'a', 'b', etc., lo pone en 0
                }
            }
        }
        return matrix;
    }
}