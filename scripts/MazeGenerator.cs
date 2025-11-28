using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : BaseProceduralGenerator
{
    [Header("Dimensiones del Laberinto")]
    [Tooltip("El ancho debe ser un número impar.")]
    public int mapWidth = 21;
    [Tooltip("La altura debe ser un número impar.")]
    public int mapHeight = 15;

    [Header("Configuración del Laberinto")]
    [Tooltip("Probabilidad (0-1) de añadir una 'cobertura' en un pasillo.")]
    [Range(0, 1)]
    public float coverChance = 0.1f;

    [Tooltip("Probabilidad (0-1) de 'romper' un muro para crear más caminos.")]
    [Range(0, 1)]
    public float breakWallChance = 0.05f;

    private int[,] matrix;
    private bool[,] visited;
    private Stack<Vector2Int> path = new Stack<Vector2Int>();

    public override int[,] GenerateMap(DifficultyLevel difficulty)
    {
        // Asegura que las dimensiones sean impares para que el laberinto funcione
        if (mapWidth % 2 == 0) mapWidth++;
        if (mapHeight % 2 == 0) mapHeight++;

        matrix = new int[mapHeight, mapWidth];
        visited = new bool[mapHeight, mapWidth];

        // 1. Llenar el mapa de muros 
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                matrix[y, x] = 1; // 1 = Muro
            }
        }

        // 2. Empezar a "cavar" el laberinto desde (1, 1)
        Vector2Int startPos = new Vector2Int(1, 1);
        visited[1, 1] = true;
        matrix[1, 1] = 0; // 0 = Pasillo
        path.Push(startPos);

        while (path.Count > 0)
        {
            Vector2Int current = path.Peek();
            List<Vector2Int> neighbours = GetUnvisitedNeighbours(current);

            if (neighbours.Count > 0)
            {
                // Hay un vecino, elegir uno al azar
                Vector2Int chosen = neighbours[Random.Range(0, neighbours.Count)];

                // Moverse al vecino y cavar el muro entre ellos
                Vector2Int wallToRemove = (chosen + current) / 2;
                matrix[wallToRemove.y, wallToRemove.x] = 0;
                
                visited[chosen.y, chosen.x] = true;
                matrix[chosen.y, chosen.x] = 0;
                path.Push(chosen);
            }
            else
            {
                // No hay vecinos, retroceder (Backtrack)
                path.Pop();
            }
        }
        
        // 3. Post-Procesado: Añadir spawns, coberturas y romper muros
        AddSpawnsAndCovers(difficulty);

        return matrix;
    }

    private List<Vector2Int> GetUnvisitedNeighbours(Vector2Int pos)
    {
        List<Vector2Int> neighbours = new List<Vector2Int>();
        // Direcciones (Arriba, Abajo, Izquierda, Derecha)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int nPos = pos + dir * 2; // Moverse 2 celdas
            
            // Comprobar si está dentro de los límites
            if (nPos.x > 0 && nPos.x < mapWidth - 1 && nPos.y > 0 && nPos.y < mapHeight - 1)
            {
                // Comprobar si no ha sido visitado
                if (!visited[nPos.y, nPos.x])
                {
                    neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }

    private void AddSpawnsAndCovers(DifficultyLevel difficulty) // <-- Acepta la dificultad
    {
        // Guarda todos los pasillos vacíos
        List<Vector2Int> openSpaces = new List<Vector2Int>();

        for (int y = 1; y < mapHeight - 1; y++)
        {
            for (int x = 1; x < mapWidth - 1; x++)
            {
                if (matrix[y, x] == 0) // Si es un pasillo
                {
                    if (Random.value < coverChance)
                    {
                        matrix[y, x] = 2;
                    } 
                    else 
                    {
                        openSpaces.Add(new Vector2Int(x, y)); // Guardar espacio vacío
                    }
                }
                else if (matrix[y, x] == 1) // Si es un muro
                {
                    if (Random.value < breakWallChance)
                    {
                        matrix[y, x] = 0; // Rompe el muro
                        openSpaces.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        // --- LÓGICA DE SPAWN MEJORADA ---
        // 1. Colocar Spawn Jugador (siempre 1)
        if (openSpaces.Count > 0) {
            int index = Random.Range(0, openSpaces.Count);
            matrix[openSpaces[index].y, openSpaces[index].x] = 8; // Jugador
            openSpaces.RemoveAt(index);
        }

        // 2. Colocar Spawns Aliados (8)
        for (int i = 0; i < difficulty.numberOfAlliesToSpawn; i++) {
            if (openSpaces.Count == 0) break;
            int index = Random.Range(0, openSpaces.Count);
            matrix[openSpaces[index].y, openSpaces[index].x] = 8; // Aliado
            openSpaces.RemoveAt(index);
        }

        // 3. Colocar Spawns Enemigos (9)
        for (int i = 0; i < difficulty.numberOfEnemiesToSpawn; i++) {
            if (openSpaces.Count == 0) break;
            int index = Random.Range(0, openSpaces.Count);
            matrix[openSpaces[index].y, openSpaces[index].x] = 9; // Enemigo
            openSpaces.RemoveAt(index);
        }
    }
}