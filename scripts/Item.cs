using UnityEngine;

// Define los tipos de items que pueden existir
public enum ItemType
{
    Health,
    Speed,
    Damage,
    Shield
}

public class Item : MonoBehaviour
{
    [Tooltip("Define qué tipo de item es este prefab.")]
    public ItemType itemType;
    
    [Header("Efectos de Recogida")] // <-- ¡AÑADE ESTA SECCIÓN!
    [Tooltip("El prefab de partículas que se instancia al recoger.")]
    public GameObject pickupEffectPrefab;
    [Tooltip("El sonido que se reproduce al recoger.")]
    public AudioClip pickupSound;
    

    [Header("Valores del Buff")]
    public float duration = 5f; // Duración para Speed/Damage/Shield
    public int healAmount = 25;
    public float speedMultiplier = 1.5f;
    public float damageMultiplier = 2.0f;
    public int shieldAmount = 50;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Intentamos obtener los componentes del objeto que nos tocó
        Health health = collision.GetComponentInParent<Health>();
        PlayerMovement playerMove = collision.GetComponentInParent<PlayerMovement>();
        TankAI tankAI = collision.GetComponentInParent<TankAI>();
        PlayerShooting playerShoot = collision.GetComponentInParent<PlayerShooting>();

        if (health == null)
        {
            return; // Si no tiene vida, no puede recoger items
        }

        // Aplicamos el efecto basado en el tipo de item
        switch (itemType)
        {
            case ItemType.Health:
                health.Heal(healAmount);
                break;

            case ItemType.Speed:
                if (playerMove != null) playerMove.ApplySpeedBuff(speedMultiplier, duration);
                if (tankAI != null) tankAI.ApplySpeedBuff(speedMultiplier, duration);
                break;

            case ItemType.Damage:
                if (playerShoot != null) playerShoot.ApplyDamageBuff(damageMultiplier, duration);
                // (Aquí podrías añadir la lógica para el buff de daño del enemigo)
                break;

            case ItemType.Shield:
                health.ApplyShield(duration); // <-- ¡Cambia 'shieldAmount' por 'duration'!
                break;
        }
        
        // 1. Reproducir el efecto de sonido (si existe)
        if (pickupSound != null)
        {
            // Esto reproduce el sonido en la posición del item
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        // 2. Crear el efecto de partículas (si existe)
        if (pickupEffectPrefab != null)
        {
            // Esto crea el prefab de partículas en la posición del item
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // Destruimos el item después de recogerlo
        Destroy(gameObject);
    }
}