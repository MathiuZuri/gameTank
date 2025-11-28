using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Ya no necesitamos un Rigidbody2D aqu�, pero no hace da�o
public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aqu� el 'FireJoystick' de la derecha.")]
    public Joystick fireJoystick;

    [Tooltip("Arrastra aqu� el prefab 'Bullet' desde Project.")]
    public GameObject bulletPrefab;

    [Tooltip("Arrastra aqu� el objeto hijo 'TurretSprite'.")]
    public Transform turretTransform; // NUEVO (reemplaza rb)

    [Tooltip("Arrastra aqu� el objeto hijo 'FirePoint' del jugador.")]
    public Transform firePoint;

    [Header("Configuraci�n de Disparo")]
    [Tooltip("Balas por segundo.")]
    public float fireRate = 2f;
    
    [Tooltip("El efecto de partículas de fuego en la punta del cañón.")]
    public GameObject cannonFireEffect;
    
    [Header("Referencias de UI de Munición")]
    [Tooltip("El 'Panel' o 'Empty' que tiene el Horizontal Layout Group.")]
    public Transform ammoLayoutGroup;
    [Tooltip("El prefab del icono de la bala (el que creaste).")]
    public GameObject ammoIconPrefab;

    [Tooltip("El icono de 'Reloj' que se muestra al recargar.")]
    public GameObject reloadingIcon;

    public int baseDamage = 10;
    
    [HideInInspector] public int magazineSize;
    [HideInInspector] public float reloadTime;

    private int currentAmmo;
    private bool isReloading = false;

    private Vector2 aimDirection;
    private float nextFireTime = 0f;
    
    private float damageMultiplier = 1.0f; // Multiplicador de daño actual

    void Start()
    {
        // Quita la referencia a rb.GetComponent
        if (fireJoystick == null)
            Debug.LogError("�No se ha asignado un FireJoystick!");
        if (bulletPrefab == null)
            Debug.LogError("�No se ha asignado un BulletPrefab!");
        if (turretTransform == null)
            Debug.LogError("�No se ha asignado 'turretTransform'!");
        if (firePoint == null)
            Debug.LogError("�No se ha asignado un FirePoint!");
        // Asegurarse de que el icono de recarga esté oculto al empezar
        if (reloadingIcon != null)
        {
            reloadingIcon.SetActive(false);
        }
    }

    void Update()
    {
        // --- 1. Apuntar (Rotacion de la Torreta) ---
        aimDirection.x = fireJoystick.Horizontal;
        aimDirection.y = fireJoystick.Vertical;

        // Solo rotamos si el joystick se est� moviendo
        if (aimDirection.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;

            // Aplicamos la rotaci�n S�LO a la torreta
            turretTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
    }
    public void InitializeAmmo()
    {
        currentAmmo = magazineSize;
        isReloading = false;
        UpdateAmmoUI();
    }
    // --- 3. NUEVA FUNCIoN PuBLICA PARA EL BOT�N ---
    public void OnFireButtonPress()
    {
        // 1. ¿Estamos recargando? Si es así, no hacer nada.
        if (isReloading)
        {
            // (Opcional: puedes poner un sonido de "clic vacío" aquí)
            return; 
        }
        // 2. ¿Podemos disparar (según la cadencia)?
        if (Time.time >= nextFireTime)
        {
            // 3. ¿Tenemos munición?
            if (currentAmmo > 0)
            {
                // ¡SÍ! Disparar.
                nextFireTime = Time.time + (1f / fireRate);
                Shoot();
            }
            else
            {
                // ¡NO! Iniciar recarga.
                StartCoroutine(Reload());
            }
        }
    }

    // Esta es la misma funci�n de antes
    void Shoot()
    {
        currentAmmo--; // Restar una bala
        // 1. Instanciamos la bala Y guardamos una referencia a ella
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    
        // 2. Obtenemos su script "Bullet" y le asignamos nuestro Tag
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.ownerTag = "Player"; // Le decimos a la bala: "Te disparó el Player"
            // Calcula el daño final usando el baseDamage + el buff
            int finalDamage = (int)(baseDamage * damageMultiplier);
            bulletScript.damageAmount = finalDamage; // Asigna el daño final a la bala
            
            // ¡NUEVO! Si el buff de daño está activo, activa el efecto de fuego
            if (damageMultiplier > 1.0f)
            {
                bulletScript.ActivateFireEffect();
            }
            
        }
        UpdateAmmoUI();
    }
    
    private IEnumerator Reload()
        {
            isReloading = true;
            Debug.Log("¡Recargando!");
            UpdateAmmoUI();
            
            // (Opcional: Iniciar animación/sonido de recarga)

            yield return new WaitForSeconds(reloadTime);
            
            currentAmmo = magazineSize;
            isReloading = false;
            UpdateAmmoUI();
            
            // (Opcional: Actualizar UI de munición aquí)
            Debug.Log("¡Recarga completa!");
        }
    public void ApplyDamageBuff(float multiplier, float duration)
    {
        StartCoroutine(DamageBuffCoroutine(multiplier, duration));
    }

    private System.Collections.IEnumerator DamageBuffCoroutine(float multiplier, float duration)
    {
        damageMultiplier = multiplier; // Aplica el buff
        
        // ¡NUEVO! Activa el fuego del cañón
        if (cannonFireEffect != null)
        {
            cannonFireEffect.SetActive(true);
        }
        
        yield return new WaitForSeconds(duration);
        
        damageMultiplier = 1.0f; // Vuelve a la normalidad
        
        // ¡NUEVO! Desactiva el fuego del cañón
        if (cannonFireEffect != null)
        {
            cannonFireEffect.SetActive(false);
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoLayoutGroup == null) return;

        // 1. Limpiar todos los iconos antiguos
        foreach (Transform icon in ammoLayoutGroup)
        {
            Destroy(icon.gameObject);
        }

        // 2. Si está recargando...
        if (isReloading)
        {
            // Mostrar el icono de recarga
            if (reloadingIcon != null)
            {
                reloadingIcon.SetActive(true);
            }
            return; // No mostrar balas
        }
        // Ocultar el icono de recarga
        if (reloadingIcon != null)
        {
            reloadingIcon.SetActive(false);
        }

        // Crear un icono por cada bala que tenemos
        for (int i = 0; i < currentAmmo; i++)
        {
            Instantiate(ammoIconPrefab, ammoLayoutGroup);
        }
    }
}