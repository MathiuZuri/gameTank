using UnityEngine;
using System.Collections.Generic;

public class CaveGenerator : BaseProceduralGenerator
{
    [Header("Dimensiones del Mapa")]
    public int mapWidth = 30;
    public int mapHeight = 20;

    [Header("Configuración de Cueva")]
    [Tooltip("Porcentaje inicial de muros (0-1). 0.45 es un buen valor.")]
    [Range(0, 1)]
    public float randomFillPercent = 0.45f;
    [Tooltip("Cuántas veces 'limpiar' el mapa. 5 es un buen valor.")]
    public int smoothingIterations = 5;
    [Tooltip("Vecinos para convertirse en muro. 4 o 5.")]
    public int wallThreshold = 4;
    [Tooltip("Vecinos para convertirse en pasillo. 3 o 4.")]
    public int openThreshold = 3;

    [Header("Configuración de Contenido")]
    [Range(0, 1)]
    public float coverChance = 0.05f;
    [Tooltip("Cuántos spawns de enemigos se crearán.")]
    public int enemySpawnCount = 5;

    private int[,] matrix;
    private bool[,] visited; // Para el algoritmo flood-fill

    public override int[,] GenerateMap(DifficultyLevel difficulty)
    {
        matrix = new int[mapHeight, mapWidth];
        visited = new bool[mapHeight, mapWidth]; // <-- NUEVO
        
        RandomFillMap();

        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }
        ProcessMap();
        PlaceSpawnsAndCovers(difficulty);

        return matrix;
    }

    void RandomFillMap()
    {
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    matrix[y, x] = 1;
                }
                else
                {
                    matrix[y, x] = (Random.value < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        // ... (Esta función se queda exactamente igual que antes) ...
        int[,] newMatrix = new int[mapHeight, mapWidth];
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                int wallCount = GetSurroundingWallCount(x, y);
                if (wallCount > wallThreshold)
                    newMatrix[y, x] = 1;
                else if (wallCount < openThreshold)
                    newMatrix[y, x] = 0;
                else
                    newMatrix[y, x] = matrix[y, x];
            }
        }
        matrix = newMatrix;
    }

    int GetSurroundingWallCount(int x, int y)
    {
        // ... (Esta función se queda exactamente igual que antes) ...
        int wallCount = 0;
        for (int ny = y - 1; ny <= y + 1; ny++) {
            for (int nx = x - 1; nx <= x + 1; nx++) {
                if (nx != x || ny != y) {
                    if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight)
                        wallCount++;
                    else
                        wallCount += matrix[ny, nx];
                }
            }
        }
        return wallCount;
    }
    // Encuentra todas las "islas" y rellena las pequeñas
    void ProcessMap()
    {
        List<List<Vector2Int>> allRegions = GetAllRegions(0); // 0 = Pasillo

        if (allRegions.Count == 0)
        {
            Debug.LogError("CaveGenerator: ¡No se generaron espacios abiertos! Prueba un 'randomFillPercent' más bajo.");
            return;
        }

        // Encontrar la región más grande
        List<Vector2Int> largestRegion = allRegions[0];
        foreach(List<Vector2Int> region in allRegions)
        {
            if (region.Count > largestRegion.Count)
            {
                largestRegion = region;
            }
        }

        // Rellenar todas las demás regiones (las islas pequeñas)
        foreach(List<Vector2Int> region in allRegions)
        {
            if (region != largestRegion)
            {
                foreach(Vector2Int tile in region)
                {
                    matrix[tile.y, tile.x] = 1; // 1 = Muro
                }
            }
        }
    }
    // Coloca spawns y coberturas SOLO en la isla principal
    void PlaceSpawnsAndCovers(DifficultyLevel difficulty) // <-- Acepta la dificultad
    {
        List<List<Vector2Int>> openRegions = GetAllRegions(0);
        if (openRegions.Count == 0) return; 

        List<Vector2Int> mainRegion = openRegions[0];
        List<Vector2Int> spawnCandidates = new List<Vector2Int>(mainRegion);

        // 1. Colocar Spawn de Jugador (siempre 1)
        if (spawnCandidates.Count > 0)
        {
            int index = Random.Range(0, spawnCandidates.Count);
            matrix[spawnCandidates[index].y, spawnCandidates[index].x] = 8;
            spawnCandidates.RemoveAt(index);
        }
        // 2. Colocar Spawns de Aliados (8)
        for (int i = 0; i < difficulty.numberOfAlliesToSpawn; i++)
        {
            if (spawnCandidates.Count == 0) break; // Sin espacio
            int index = Random.Range(0, spawnCandidates.Count);
            matrix[spawnCandidates[index].y, spawnCandidates[index].x] = 8;
            spawnCandidates.RemoveAt(index);
        }

        // 3. Colocar Spawns de Enemigos (9)
        for (int i = 0; i < difficulty.numberOfEnemiesToSpawn; i++) // Usa la dificultad
        {
            if (spawnCandidates.Count == 0) break; 
            int index = Random.Range(0, spawnCandidates.Count);
            matrix[spawnCandidates[index].y, spawnCandidates[index].x] = 9; 
            spawnCandidates.RemoveAt(index);
        }

        // 4. Colocar Coberturas (2) en el espacio restante
        foreach(Vector2Int pos in spawnCandidates)
        {
            if (Random.value < coverChance)
            {
                matrix[pos.y, pos.x] = 2; // Cobertura
            }
        }
    }
    // Devuelve una lista de todas las regiones de un tipo de celda
    List<List<Vector2Int>> GetAllRegions(int tileType)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        visited = new bool[mapHeight, mapWidth]; // Resetear visitados

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (matrix[y, x] == tileType && !visited[y, x])
                {
                    List<Vector2Int> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                }
            }
        }
        return regions;
    }

    // Algoritmo "Flood-Fill"
    List<Vector2Int> GetRegionTiles(int startX, int startY)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startY, startX] = true;

        while(queue.Count > 0)
        {
            Vector2Int tile = queue.Dequeue();
            tiles.Add(tile);

            // Revisar vecinos (Arriba, Abajo, Izquierda, Derecha)
            Vector2Int[] neighbours = {
                new Vector2Int(tile.x, tile.y + 1),
                new Vector2Int(tile.x, tile.y - 1),
                new Vector2Int(tile.x + 1, tile.y),
                new Vector2Int(tile.x - 1, tile.y)
            };

            foreach (var n in neighbours)
            {
                // Si está dentro del mapa
                if (n.x >= 0 && n.x < mapWidth && n.y >= 0 && n.y < mapHeight)
                {
                    // Si es del mismo tipo y no lo hemos visitado
                    if (matrix[n.y, n.x] == 0 && !visited[n.y, n.x])
                    {
                        visited[n.y, n.x] = true;
                        queue.Enqueue(n);
                    }
                }
            }
        }
        return tiles;
    }
}