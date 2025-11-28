using System.Collections.Generic;
using UnityEngine;

public class RandomFillGenerator : BaseProceduralGenerator
{
    [Header("Dimensiones")]
    public int mapWidth = 20;
    public int mapHeight = 15;

    [Header("Configuración de Celdas")]
    [Tooltip("Probabilidad (0-1) de que una celda sea una Cobertura (2).")]
    [Range(0, 1)]
    public float coverChance = 0.1f; 

    [Tooltip("Probabilidad (0-1) de que una celda vacía se convierta en Spawn de Enemigo (9).")]
    [Range(0, 1)]
    public float enemySpawnChance = 0.05f; // 5% de chance de ser spawn enemigo

    [Header("Configuración de Spawns")]
    [Tooltip("Margen desde el borde para spawns de Jugador/Enemigo.")]
    public int spawnBorderMargin = 2;

    public override int[,] GenerateMap(DifficultyLevel difficulty)
    {
        int[,] matrix = new int[mapHeight, mapWidth];
        
        // Listas para guardar espacios vacíos
        List<Vector2Int> emptySpaces = new List<Vector2Int>();

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // 1. Poner los bordes
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    matrix[y, x] = 1; // Muro
                }
                // 2. Relleno aleatorio
                else
                {
                    if (Random.value < coverChance)
                    {
                        matrix[y, x] = 2; // Cobertura
                    }
                    else
                    {
                        matrix[y, x] = 0; // Vacío
                        emptySpaces.Add(new Vector2Int(x, y)); // Guardar el espacio
                    }
                }
            }
        }
        // Coloca al jugador
        if (emptySpaces.Count > 0)
        {
            int index = Random.Range(0, emptySpaces.Count);
            matrix[emptySpaces[index].y, emptySpaces[index].x] = 8; // Jugador
            emptySpaces.RemoveAt(index);
        }

        // Coloca Aliados (basado en la dificultad)
        for (int i = 0; i < difficulty.numberOfAlliesToSpawn; i++)
        {
            if (emptySpaces.Count == 0) break; // No hay más espacio
            int index = Random.Range(0, emptySpaces.Count);
            matrix[emptySpaces[index].y, emptySpaces[index].x] = 8; // Aliado
            emptySpaces.RemoveAt(index);
        }
        
        // Coloca Enemigos (basado en la dificultad)
        for (int i = 0; i < difficulty.numberOfEnemiesToSpawn; i++)
        {
            if (emptySpaces.Count == 0) break; // No hay más espacio
            int index = Random.Range(0, emptySpaces.Count);
            matrix[emptySpaces[index].y, emptySpaces[index].x] = 9; // Enemigo
            emptySpaces.RemoveAt(index);
        }

        return matrix;
    }
}