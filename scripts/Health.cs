using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Tooltip("La salud máxima.")]
    public int maxHealth = 100;

    // (Opcional) Evento que se dispara al morir
    public UnityEvent OnDie;

    [Header("UI del Jugador (Asignar solo en el Player)")]
    [Tooltip("Arrastra aquí el 'HealthSlider' de la UI principal.")]
    public Slider playerHealthSlider;

    [Header("UI del Enemigo (Asignar solo en el Enemy)")]
    [Tooltip("Arrastra aquí el 'HealthBarCanvas' del prefab del enemigo.")]
    public GameObject enemyHealthBarCanvas;
    [Tooltip("Tiempo que la barra de vida es visible después de recibir daño.")]
    public float healthBarVisibilityTime = 5f;
    private Slider enemyHealthSlider;
    private Coroutine healthBarCoroutine;
    
    [Header("Efectos de Muerte (Asignar solo en el Enemy)")]
    [Tooltip("Arrastra aquí el 'Scrap_Prefab' en el que se convierte al morir.")]
    public GameObject scrapPrefab;
    
    [Tooltip("Arrastra aquí el 'BaseSprite' para saber la rotación de la chatarra.")]
    public Transform baseTransform; // <-- ¡AÑADE ESTA LÍNEA!

    [Tooltip("El prefab del efecto de moneda que aparece al morir.")]
    public GameObject coinDropEffectPrefab;
    [Tooltip("Cantidad de monedas que da al morir (si es un enemigo).")]
    public int coinsOnDeath = 20;

    [Header("Efectos")]
    [Tooltip("Arrastra aquí el objeto 'ShieldVisual' que es hijo del jugador/enemigo.")]
    public GameObject shieldVisual;
    
    [Header("Efectos de Poca Vida")]
    public GameObject lowHealthSmokeEffect;
    public GameObject lowHealthFireEffect;
    [Tooltip("Valor de vida exacto para activar los efectos (ej. 30).")]
    public int lowHealthThreshold = 30; // Modificado de float a int
    private bool isLowHealth = false; 

    [Header("Efectos de Curación")]
    public GameObject healAuraVisual;
    public GameObject healParticleEffectPrefab;
    public float healEffectDuration = 2f;

    // Variable privada para almacenar la salud actual
    private int currentHealth;
    private bool isShielded;
    private TankAI aiController;
    public void Initialize()
    {
        currentHealth = maxHealth;
        // Busca el SaveManager en la escena para poder darle moneda
        aiController = GetComponent<TankAI>();
        // Configurar la UI del Jugador
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = maxHealth;
            playerHealthSlider.value = currentHealth;
        }
        // Configurar la UI del Enemigo
        if (enemyHealthBarCanvas != null)
        {
            enemyHealthSlider = enemyHealthBarCanvas.GetComponentInChildren<Slider>();
            if (enemyHealthSlider != null) // Añade esta comprobación por si acaso
            {
                enemyHealthSlider.maxValue = maxHealth;
                enemyHealthSlider.value = currentHealth;
                enemyHealthBarCanvas.SetActive(false);
            }
        }
        CheckLowHealthStatus();
    }
    public void TakeDamage(int amount, string killerTag = null, Transform bulletTransform = null)
    {
        if (isShielded)
        {
            Debug.Log(gameObject.name + " bloqueó el daño (Inmortal).");
            return; // No recibir daño
        }
        if (currentHealth <= 0)
        {
            return;
        }
        currentHealth -= amount;
        Debug.Log(gameObject.name + " recibió " + amount + " de daño. Vida restante: " + currentHealth);
        // Si somos una IA y nos dispararon, ¡investigar!
        if (aiController != null && bulletTransform != null)
        {
            aiController.OnTookDamage(bulletTransform);
        }
        // 1. Actualizar Slider del Jugador (si existe)
        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = currentHealth;
        }
        // 2. Mostrar y actualizar Barra de Vida del Enemigo (si existe)
        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.value = currentHealth;
            // Reiniciar el timer de 5 segundos
            if (healthBarCoroutine != null)
                StopCoroutine(healthBarCoroutine);
            healthBarCoroutine = StartCoroutine(ShowHealthBar());
        }
        if (currentHealth <= 0)
        {
            Die(killerTag);
        }
        CheckLowHealthStatus();
    }
    private void Die(string killerTag) // Acepta el tag del asesino
    {
        Debug.Log(gameObject.name + " ha muerto.");
        OnDie?.Invoke();
        // Si este objeto es un "Enemy" Y fue asesinado por el "Player"
        if (gameObject.CompareTag("Enemy") && killerTag == "Player")
        {
            // 1. Dar monedas
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.GiveCoins(coinsOnDeath);
            }
            // 2. Mostrar efecto visual
            if (coinDropEffectPrefab != null)
            {
                Instantiate(coinDropEffectPrefab, transform.position, Quaternion.identity);
            }
        }
        // Convertirse en Chatarra
        if (scrapPrefab != null)
        {
            Instantiate(scrapPrefab, transform.position, baseTransform.rotation);
        }
        Destroy(gameObject);
    }
    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return; 
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        // Actualizar UI de vida del jugador
        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = currentHealth;
        }
        Debug.Log(gameObject.name + " se curó " + amount);
        StartCoroutine(HealEffectsCoroutine());
        CheckLowHealthStatus();
    }
    private IEnumerator ShowHealthBar()
    {
        enemyHealthBarCanvas.SetActive(true);
        yield return new WaitForSeconds(healthBarVisibilityTime);
        enemyHealthBarCanvas.SetActive(false);
    }
    // Ahora acepta una "duración" en lugar de una "cantidad"
    public void ApplyShield(float duration)
    {
        // Si ya tenemos un escudo activo, no hacemos nada (o reiniciamos el timer)
        if (isShielded)
        {
            // Opcional: reiniciar el timer
            StopCoroutine("ShieldCoroutine"); // Detiene el anterior
        }
        Debug.Log(gameObject.name + " obtuvo un escudo por " + duration + " seg.");
        StartCoroutine(ShieldCoroutine(duration));
    }
    private System.Collections.IEnumerator ShieldCoroutine(float duration)
    {
        isShielded = true;
        // Activar el efecto visual
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }
        // Esperar el tiempo de duración
        yield return new WaitForSeconds(duration);
        // Desactivar el escudo
        isShielded = false;
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
        Debug.Log(gameObject.name + " perdió el escudo.");
    }
    private System.Collections.IEnumerator HealEffectsCoroutine()
    {
        // 1. Activar el aura (si existe)
        if (healAuraVisual != null)
        {
            healAuraVisual.SetActive(true);
        }
        // 2. Instanciar las cruces (si existen)
        if (healParticleEffectPrefab != null)
        {
            // ¡ESTE ES EL CÓDIGO QUE FALTABA!
            GameObject effect = Instantiate(healParticleEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform); // Para que se mueva con el tanque
        }
        // 3. Esperar el tiempo que dura el aura
        yield return new WaitForSeconds(healEffectDuration);
        // 4. Desactivar el aura
        if (healAuraVisual != null)
        {
            healAuraVisual.SetActive(false);
        }
    }
    //LÓGICA DE POCA VIDA
    void CheckLowHealthStatus()
    {
        // Compara la vida actual con el umbral (ya no es porcentaje)
        bool shouldBeLowHealth = (currentHealth <= lowHealthThreshold);

        if (shouldBeLowHealth == isLowHealth)
            return;
        isLowHealth = shouldBeLowHealth;

        if (lowHealthSmokeEffect != null)
        {
            lowHealthSmokeEffect.SetActive(isLowHealth);
        }
        if (lowHealthFireEffect != null)
        {
            lowHealthFireEffect.SetActive(isLowHealth);
        }
    }
}