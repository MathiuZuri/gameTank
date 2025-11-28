using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Referencias de Componentes")]
    public Transform baseTransform;
    public Transform turretTransform;
    public PlayerMovement playerMovement; // Arrastra tu script de movimiento aquí
    public PlayerShooting playerShooting; // Arrastra tu script de disparo aquí
    public TankAI tankAI;             // Arrastra tu script de IA aquí
    public Health health;               // Arrastra tu script de vida aquí

    [Header("Datos del Tanque")]
    public TankData currentTankData; // ¡Aquí va el asset!

    void Start()
    {
        if (currentTankData != null)
        {
            ApplyTankData(currentTankData);
        }
        else if (health != null) // Si no hay datos (como el Player al inicio)
        {
            health.Initialize(); // Inicializa con los valores por defecto
        }
    }

    // Esta función aplica los datos del ScriptableObject a los componentes
    public void ApplyTankData(TankData data)
    {
        currentTankData = data;
        if (data == null) return;
        TankVisuals visualsToApply;
        // Determinamos qué set de sprites usar
        if (playerMovement != null) // Si es el Jugador
        {
            visualsToApply = data.playerVisuals;
        }
        else if (tankAI != null && tankAI.team == AITeam.Ally) // Si es un Aliado
        {
            visualsToApply = data.playerVisuals; // <-- ¡USA SPRITES DE JUGADOR!
        }
        else // Si es un Enemigo
        {
            visualsToApply = data.enemyVisuals;
        }

        // Aplicar Visuales
        if (baseTransform != null) 
            baseTransform.GetComponent<SpriteRenderer>().sprite = visualsToApply.baseSprite;
        if (turretTransform != null) 
            turretTransform.GetComponent<SpriteRenderer>().sprite = visualsToApply.turretSprite;
        // Aplicar Estadísticas de Vida
        if (health != null)
        {
            health.maxHealth = data.maxHealth;
            health.Initialize();
        }
        // Aplicar Estadísticas a los scripts (Jugador o Enemigo)
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = data.moveSpeed;
            playerMovement.InitializeSpeed();
        }
            
        if (playerShooting != null)
        {
            playerShooting.fireRate = data.fireRate;
            playerShooting.bulletPrefab = data.bulletPrefab;
            playerShooting.baseDamage = data.bulletDamage;
            playerShooting.magazineSize = data.magazineSize;
            playerShooting.reloadTime = data.reloadTime;
            playerShooting.InitializeAmmo();
        }
        
        if (tankAI != null)
        {
            tankAI.moveSpeed = data.moveSpeed;
            tankAI.InitializeSpeed();
            tankAI.fireRate = data.fireRate;
            tankAI.bulletPrefab = data.bulletPrefab;
            tankAI.baseDamage = data.bulletDamage;
            tankAI.magazineSize = data.magazineSize;
            tankAI.reloadTime = data.reloadTime;
            tankAI.InitializeAmmo();
        }
    }
}