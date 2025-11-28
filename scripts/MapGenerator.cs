//public CinemachineConfiner2D cinemachineConfiner; // Se llamaba CinemachineConfiner2D
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine; // Correcto para Unity 6

public enum MapSource
{
    FromAsset,
    Procedural
}

public class MapGenerator : MonoBehaviour
{
    [Header("Configuración del Mapa")]
    public MapSource mapSource;

    [Header("Prefabs del Nivel")]
    public GameObject wallPrefab;
    public GameObject coverPrefab;
    public GameObject genericEnemyPrefab;
    public GameObject genericAllyPrefab;

    [Header("Referencias de Objetos")]
    public Transform playerTransform;

    [Header("Referencias de Cámara")]

    [Tooltip("Arrastra aquí el componente 'Confiner 2D' de tu cámara virtual.")]
    public CinemachineConfiner2D cinemachineConfiner;

    [Header("Configuración del Nivel")]
    public float tileSize = 1.0f;

    [Header("Dependencias")]
    public DifficultyDatabase difficultyDatabase;
    public MapDatabase mapDatabase;
    public List<BaseProceduralGenerator> proceduralGenerators;

    [Header("Referencias de Spawns")]
    public List<Vector2> emptySpaces = new List<Vector2>();
    // Esta lista la leerá el GameManager
    public List<TankAI> spawnedEnemies = new List<TankAI>();

    // --- Variables Privadas ---
    private Transform mapHolder;
    private List<Vector2> playerSpawnPoints = new List<Vector2>();
    private List<Vector2> enemySpawnPoints = new List<Vector2>();
    private int[,] mapMatrix;
    private SaveManager saveManager;
    private DifficultyLevel currentDifficulty;
    private MapData currentMap;
    
    void Start()
    {
        // 1. Cargar Dificultad y Mapa
        if (SaveManager.Instance == null)
        {
            Debug.LogError("¡No se encontró el SaveManager!");
            return; // Añade un 'return' para seguridad
        }
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame();
        string difficultyID = SaveManager.Instance.playerData.selectedDifficultyID;
        currentDifficulty = difficultyDatabase.GetDifficultyByID(difficultyID);
        if (currentDifficulty == null)
        {
            Debug.LogError("¡No se pudo cargar la dificultad! Usando la primera por defecto.");
            currentDifficulty = difficultyDatabase.allDifficulties[0];
        }
        // ¡USA EL SINGLETON AQUÍ!
        string mapID = SaveManager.Instance.playerData.selectedMapID;
        // 2. Comprobar si el ID es de un mapa PROCEDURAL
        if (mapID.StartsWith("__"))
        {
            mapSource = MapSource.Procedural;
            BaseProceduralGenerator generatorToUse = null;
            foreach (BaseProceduralGenerator gen in proceduralGenerators)
            {
                if (gen.generatorID == mapID)
                {
                    generatorToUse = gen;
                    break;
                }
            }
            if (generatorToUse != null)
            {
                mapMatrix = generatorToUse.GenerateMap(currentDifficulty);
            }
            else
            {
                Debug.LogError("¡Mapa procedural no encontrado! ID: " + mapID);
                return;
            }
        }
        else // 3. Si no, es un mapa de ASSET
        {
            mapSource = MapSource.FromAsset;
            currentMap = mapDatabase.GetMapByID(mapID);
            if (currentMap == null)
            {
                Debug.LogError("¡Mapa de asset no encontrado! ID: " + mapID);
                return;
            }
            mapMatrix = currentMap.GetMapMatrix();
        }
    }
    public void GenerateLevel()
    {
        if (mapHolder != null) Destroy(mapHolder.gameObject);
        mapHolder = new GameObject("MapHolder").transform;
        playerSpawnPoints.Clear();
        enemySpawnPoints.Clear();
        emptySpaces.Clear();
        int rows = mapMatrix.GetLength(0);
        int cols = mapMatrix.GetLength(1);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int tileType = mapMatrix[y, x];
                Vector2 position = new Vector2(x * tileSize, (rows - 1 - y) * tileSize)
                                 + new Vector2(0.5f * tileSize, 0.5f * tileSize);

                GameObject prefabToSpawn = null;
                switch (tileType)
                {
                    case 1: prefabToSpawn = wallPrefab; break;
                    case 2: prefabToSpawn = coverPrefab; break;
                    case 8: playerSpawnPoints.Add(position); break;
                    case 9: enemySpawnPoints.Add(position); break;
                    case 0: emptySpaces.Add(position); break;
                }
                if (prefabToSpawn != null)
                {
                    GameObject instance = Instantiate(prefabToSpawn, position, Quaternion.identity);
                    instance.transform.SetParent(mapHolder);
                }
            }
        }

        #region Crear Límites de Cámara
        if (cinemachineConfiner != null)
        {
            float mapWorldWidth = cols * tileSize;
            float mapWorldHeight = rows * tileSize;
            GameObject boundsObj = new GameObject("MapBounds");
            boundsObj.transform.SetParent(mapHolder);
            PolygonCollider2D boundsCollider = boundsObj.AddComponent<PolygonCollider2D>();
            boundsCollider.isTrigger = true;
            Vector2[] colliderPoints = new Vector2[4];
            colliderPoints[0] = new Vector2(0, 0);
            colliderPoints[1] = new Vector2(0, mapWorldHeight);
            colliderPoints[2] = new Vector2(mapWorldWidth, mapWorldHeight);
            colliderPoints[3] = new Vector2(mapWorldWidth, 0);
            boundsCollider.SetPath(0, colliderPoints);

            cinemachineConfiner.BoundingShape2D = boundsCollider;
            cinemachineConfiner.InvalidateBoundingShapeCache();
        }
        else
        {
            Debug.LogWarning("No se asignó un Confiner2D al MapGenerator.");
        }
        #endregion
        
    }
    public void SpawnPlayer()
    {
        if (playerTransform == null) { return; }
        if (playerSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No se encontraron 'Player Spawns' (8). Spawneando en el centro.");
            playerTransform.position = new Vector2((mapMatrix.GetLength(1) * tileSize) / 2, (mapMatrix.GetLength(0) * tileSize) / 2);
            return;
        }

        int index = Random.Range(0, playerSpawnPoints.Count);
        playerTransform.position = playerSpawnPoints[index];
        playerSpawnPoints.RemoveAt(index);
    }
    public List<TankAI> SpawnEnemies()
    {
        // Limpiar la lista de la ronda anterior
        spawnedEnemies.Clear();

        // 1. Validar
        if (genericEnemyPrefab == null) { /* ... */ return spawnedEnemies; }
        if (currentDifficulty == null) { /* ... */ return spawnedEnemies; }
        if (currentDifficulty.enemyTankTypes.Count == 0) { /* ... */ return spawnedEnemies; }
        if (enemySpawnPoints.Count == 0) { /* ... */ return spawnedEnemies; }

        // 2. Preparar listas
        List<Vector2> availableSpawns = new List<Vector2>(enemySpawnPoints);
        List<TankData> tanksToUse = currentDifficulty.enemyTankTypes;
        int enemiesToSpawn = Mathf.Min(currentDifficulty.numberOfEnemiesToSpawn, availableSpawns.Count);

        // 3. Bucle de Spawneo
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            int spawnIndex = Random.Range(0, availableSpawns.Count);
            Vector2 spawnPos = availableSpawns[spawnIndex];
            availableSpawns.RemoveAt(spawnIndex);

            TankData tankTypeToSpawn = tanksToUse[Random.Range(0, tanksToUse.Count)];
            GameObject enemyInstance = Instantiate(genericEnemyPrefab, spawnPos, Quaternion.identity);
            enemyInstance.transform.SetParent(mapHolder);

            TankController controller = enemyInstance.GetComponent<TankController>();
            if (controller != null)
            {
                controller.ApplyTankData(tankTypeToSpawn);

                // ¡Añade la IA a la lista que devolveremos!
                spawnedEnemies.Add(enemyInstance.GetComponent<TankAI>());
            }
        }

        if (currentDifficulty.numberOfEnemiesToSpawn > enemiesToSpawn) { /* ... */ }

        // 4. Devolver la lista al GameManager
        return spawnedEnemies;
    }
    
    public void SpawnAllies()
    {
        if (genericAllyPrefab == null) { /* ... */ return; }
        if (currentDifficulty == null || currentDifficulty.numberOfAlliesToSpawn == 0) { return; }
        if (currentDifficulty.allyTankTypes.Count == 0) { /* ... */ return; }
        if (playerSpawnPoints.Count == 0) { /* ... */ return; }

        List<Vector2> availableSpawns = new List<Vector2>(playerSpawnPoints);
        List<TankData> tanksToUse = currentDifficulty.allyTankTypes;
        int alliesToSpawn = Mathf.Min(currentDifficulty.numberOfAlliesToSpawn, availableSpawns.Count);

        for (int i = 0; i < alliesToSpawn; i++)
        {
            int spawnIndex = Random.Range(0, availableSpawns.Count);
            Vector2 spawnPos = availableSpawns[spawnIndex];
            availableSpawns.RemoveAt(spawnIndex);

            TankData tankTypeToSpawn = tanksToUse[Random.Range(0, tanksToUse.Count)];
            GameObject allyInstance = Instantiate(genericAllyPrefab, spawnPos, Quaternion.identity);
            allyInstance.transform.SetParent(mapHolder);

            TankController controller = allyInstance.GetComponent<TankController>();
            if (controller != null)
            {
                controller.ApplyTankData(tankTypeToSpawn);
            }
        }
    }
}