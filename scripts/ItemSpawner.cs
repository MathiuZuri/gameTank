using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Configuración del Spawner")]
    [Tooltip("Cantidad máxima de items en el mapa a la vez.")]
    public int maxConcurrentItems = 5;
    
    [Tooltip("Tiempo (en segundos) entre cada intento de spawn.")]
    public float spawnInterval = 10.0f;
    
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el MapGenerator para que pueda leer los espacios vacíos.")]
    public MapGenerator mapGenerator;
    
    [Tooltip("Arrastra aquí TODOS tus prefabs de items (Health, Speed, etc.).")]
    public List<GameObject> itemPrefabs;

    // Lista interna para rastrear los items
    private List<GameObject> activeItems = new List<GameObject>();
    private float nextSpawnTime;

    void Update()
    {
        // Limpiamos la lista de items que ya fueron recogidos
        activeItems.RemoveAll(item => item == null);

        // Comprobamos si es hora de spawnear
        if (Time.time > nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            
            // Intentamos spawnear si no hemos llegado al límite
            if (activeItems.Count < maxConcurrentItems && itemPrefabs.Count > 0)
            {
                SpawnRandomItem();
            }
        }
    }

    void SpawnRandomItem()
    {
        if (mapGenerator.emptySpaces.Count == 0)
        {
            Debug.LogWarning("ItemSpawner: ¡No hay espacios vacíos para spawnear items!");
            return;
        }

        // 1. Elegir un item aleatorio de la lista de prefabs
        GameObject prefabToSpawn = itemPrefabs[Random.Range(0, itemPrefabs.Count)];

        // 2. Elegir una posición aleatoria de la lista de espacios vacíos
        Vector2 spawnPos = mapGenerator.emptySpaces[Random.Range(0, mapGenerator.emptySpaces.Count)];

        // 3. Instanciar el item y añadirlo a nuestra lista
        GameObject newItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        activeItems.Add(newItem);
    }
}