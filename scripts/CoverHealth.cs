using UnityEngine;

public class CoverHealth : MonoBehaviour
{
    [Tooltip("La salud máxima de la cobertura.")]
    public int maxHealth = 50;
    private int currentHealth;
    void Start()
    {
        currentHealth = maxHealth;
    }
    // Esta es la función pública que llamará la bala
    public void TakeDamage(int amount)
    {
        // Si ya está destruido, no hacer nada
        if (currentHealth <= 0)
        {
            return;
        }
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        // Simplemente se destruye. Sin chatarra, sin monedas.
        Destroy(gameObject);
    }
}