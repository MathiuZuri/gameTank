using UnityEngine;

// ¡NUEVO! Un struct para agrupar los sprites
[System.Serializable]
public struct TankVisuals
{
    public Sprite baseSprite;
    public Sprite turretSprite;
}

[CreateAssetMenu(fileName = "NewTank", menuName = "Tanques/Tank Data")]
public class TankData : ScriptableObject
{
    [Header("Identificación")]
    public string tankID; 
    public string tankName; 
    
    [TextArea(3, 5)]
    public string description;
    
    [Header("Componentes Visuales")]
    public TankVisuals playerVisuals;  // Sprites para el Jugador
    public TankVisuals enemyVisuals;   // Sprites para el Enemigo

    [Header("Estadísticas de Combate")]
    public int maxHealth = 100;
    public float fireRate = 2f;
    public GameObject bulletPrefab;
    public int bulletDamage = 10;
    
    [Tooltip("Tamaño del cargador (cuántas balas puede disparar antes de recargar).")]
    public int magazineSize = 10;
    [Tooltip("Tiempo en segundos que tarda en recargar.")]
    public float reloadTime = 2f;
    
    [Header("Estadísticas de Movimiento")]
    public float moveSpeed = 5f;

    [Header("Tienda")]
    public int shopPrice = 100;
}