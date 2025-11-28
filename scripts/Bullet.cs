using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Configuración")]
    public float bulletSpeed = 5f; // Ajustado al valor de tu inspector
    public float lifeTime = 3.0f;
    public int damageAmount = 10;
    
    [Tooltip("El efecto de partículas de fuego que sigue a la bala.")]
    public GameObject fireEffectVisual;

    [Header("Identificación")]
    public string ownerTag; 

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = transform.up * bulletSpeed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Obtener los posibles objetivos
        Health targetHealth = collision.GetComponentInParent<Health>();
        CoverHealth coverHealth = collision.GetComponentInParent<CoverHealth>(); 

        // 2. Comprobar si golpeamos a nuestro propio dueño
        if (targetHealth != null && targetHealth.CompareTag(ownerTag))
        {
            return; // Es nuestro dueño, no hacer nada.
        }
        
        // 3. Comprobar si un ALIADO golpea al JUGADOR
        if (ownerTag == "Ally" && collision.CompareTag("Player"))
        {
            return; // Fuego amigo, no hacer nada.
        }
        
        // 4. Comprobar si el JUGADOR golpea a un ALIADO
        if (ownerTag == "Player" && collision.CompareTag("Ally"))
        {
            return; // Fuego amigo, no hacer nada.
        }

        // 5. Comprobar si golpeamos un Muro (Wall)
        bool isWall = collision.CompareTag("Wall") || (collision.transform.parent != null && collision.transform.parent.CompareTag("Wall"));
        if (isWall)
        {
            Destroy(gameObject); 
            return;
        }

        // 6. Comprobar si golpeamos una Cobertura (Cover)
        if (coverHealth != null)
        {
            coverHealth.TakeDamage(damageAmount);
            Destroy(gameObject); 
            return;
        }
        
        // 7. Comprobar si golpeamos un objetivo con vida (Enemigo, Jugador golpeado por Enemigo, etc.)
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damageAmount, ownerTag, transform); 
            Destroy(gameObject);
            return;
        }
    }
    
    // Esta función la llamará el script de Disparo
    public void ActivateFireEffect()
    {
        if (fireEffectVisual != null)
        {
            fireEffectVisual.SetActive(true);
        }
    }
}