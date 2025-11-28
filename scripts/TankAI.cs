using UnityEngine;
using System.Collections; // Necesario para Coroutines
using System.Collections.Generic;

public enum AITeam
{
    Enemy, // Atacará al "Player"
    Ally   // Atacará al "Enemy"
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class TankAI : MonoBehaviour
{
    [Header("Configuración del Equipo")]
    public AITeam team; // <-- ¡La nueva variable clave!

    [Header("Referencias")]
    public Transform turretTransform;
    public Transform baseTransform;
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Configuración de IA")]
    public float detectionRange = 15f;
    public float strafeDistance = 7f;
    public float moveSpeed = 3f;
    public float fireRate = 1f;
    public float baseRotationSpeed = 180f;

    [Header("Configuración de Apuntado")]
    public float turretRotationSpeed = 100f;
    public float aimInaccuracy = 1.0f;
    public float assumedBulletSpeed = 10f;
    public LayerMask visionBlockingMask;
    [Tooltip("Precisión (en grados) que debe tener el cañón antes de disparar.")]
    public float firingAngleThreshold = 10f;

    [Header("Configuración de Patrullaje")]
    public float patrolStuckTime = 10f;
    public float patrolRadius = 10f;
    public Vector2 patrolWaitTime = new Vector2(2f, 5f);

    [Header("Efectos")]
    public GameObject cannonFireEffect;

    private Transform currentTargetTransform; // Reemplaza a 'playerTransform'
    private Rigidbody2D currentTargetRb;      // Reemplaza a 'playerRb'
    private string targetTag;                 // "Player" o "Enemy"

    // Variables de Strafe
    private float timeToChangeStrafe;
    private int strafeDirection;
    private bool isBackingUp;

    // Referencias privadas
    private Rigidbody2D rb;
    private float nextFireTime;

    // Variables de Estado
    private bool isPatrolling = true;
    private bool isInvestigating = false;
    private Vector2 lastKnownPlayerPosition;

    // Variables de Patrullaje
    private Vector2 patrolTargetPosition;
    private float currentPatrolWaitTime;
    private float timeToChangePatrolTarget;
    private float stuckTimer;

    // Variables de Apuntado
    private Vector2 aimOffset;
    private float timeToChangeOffset;

    // Variables de Buffs
    private float originalSpeed;
    private MotionTrail motionTrail;
    private float damageMultiplier = 1.0f;

    // Variables de Munición
    public int baseDamage = 10;
    [HideInInspector] public int magazineSize;
    [HideInInspector] public float reloadTime;
    private int currentAmmo;
    private bool isReloading = false;

    // Temporizador de "Pensamiento"
    private float visionCheckCooldown = 0.2f; // 5 veces por seg
    private float nextVisionCheckTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        motionTrail = GetComponent<MotionTrail>();

        if (team == AITeam.Enemy)
        {
            targetTag = "Player";
        }
        else // team == AITeam.Ally
        {
            targetTag = "Enemy";
        }
        FindNewTarget(); // Intenta encontrar un objetivo inmediatamente
        SetNewPatrolTarget();
    }
    // Métodos que permiten al controlador externo inicializar stats
    public void InitializeAmmo()
    {
        currentAmmo = magazineSize;
        isReloading = false;
    }

    public void InitializeSpeed()
    {
        originalSpeed = moveSpeed;
    }
    void Update()
    {
        // 1. "Pensar" (decidir el estado) solo cada 0.2 segundos
        if (Time.time >= nextVisionCheckTime)
        {
            Think();
            nextVisionCheckTime = Time.time + visionCheckCooldown;
        }

        // 2. "Actuar" (ejecutar el estado) CADA FRAME
        if (isPatrolling)
        {
            Wander();
        }
        else
        {
            // Si nuestro objetivo se destruyó mientras atacábamos,
            // dejamos de atacar.
            if (currentTargetTransform == null)
            {
                isPatrolling = true;
                return;
            }

            AimTurret();
            MoveBase();
            TryShoot();
        }
    }
    // Ahora busca al objetivo más cercano, no solo al jugador
    void Think()
    {
        // Si actualmente no tenemos objetivo (o fue destruido) o estamos investigando, buscar uno nuevo
        if (currentTargetTransform == null || isInvestigating)
        {
            FindNewTarget();

            // Si seguimos sin encontrar, nos ponemos a patrullar.
            if (currentTargetTransform == null)
            {
                isPatrolling = true;
                isInvestigating = false;
                return;
            }
        }

        // Comprobar distancia al objetivo
        float distanceToTarget = Vector2.Distance(transform.position, currentTargetTransform.position);

        if (distanceToTarget > detectionRange)
        {
            isPatrolling = true;
            isInvestigating = false;
            return;
        }

        // Comprobar la Línea de Visión (LOS)
        Vector2 dirToTarget = (currentTargetTransform.position - firePoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dirToTarget, distanceToTarget, visionBlockingMask);
        bool canSeeTarget = (hit.collider == null);

        if (canSeeTarget)
        {
            // ¡TE VEO! -> Cambiar a modo Ataque
            isPatrolling = false;
            isInvestigating = false;
            lastKnownPlayerPosition = currentTargetTransform.position; // Actualiza la memoria
        }
        else
        {
            // NO TE VEO -> Cambiar a modo Patrullar/Investigar
            if (!isPatrolling && !isInvestigating)
            {
                // ¡Acabo de perderlo! Ir a investigar.
                isInvestigating = true;
                SetNewPatrolTarget(lastKnownPlayerPosition);
            }
            isPatrolling = true;
        }
    }
    // Busca el objetivo MÁS CERCANO con el tag correcto
    void FindNewTarget()
    {
        List<GameObject> potentialTargets = new List<GameObject>();

        if (team == AITeam.Enemy)
        {
            // Un enemigo ataca al Jugador Y a los Aliados
            potentialTargets.AddRange(GameObject.FindGameObjectsWithTag("Player"));
            potentialTargets.AddRange(GameObject.FindGameObjectsWithTag("Ally"));
        }
        else // (team == AITeam.Ally)
        {
            // ¡CORREGIDO! Añade los enemigos a la lista principal
            potentialTargets.AddRange(GameObject.FindGameObjectsWithTag(targetTag));
        }

        // El resto de la función es la misma...
        if (potentialTargets.Count == 0)
        {
            currentTargetTransform = null;
            currentTargetRb = null;
            return;
        }

        Transform closestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (GameObject targetObj in potentialTargets)
        {
            if (targetObj == null) continue;
            float dist = Vector2.Distance(transform.position, targetObj.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTarget = targetObj.transform;
            }
        }

        currentTargetTransform = closestTarget;
        if (closestTarget != null)
            currentTargetRb = closestTarget.GetComponent<Rigidbody2D>();
        else
            currentTargetRb = null;
    }

    void AimTurret()
    {
        // Seguridad
        if (currentTargetTransform == null) return;

        // Reemplaza 'playerRb' con 'currentTargetRb'
        Vector2 targetVel = (currentTargetRb != null) ? currentTargetRb.velocity : Vector2.zero;

        // Reemplaza 'playerTransform' con 'currentTargetTransform'
        float distance = Vector2.Distance(transform.position, currentTargetTransform.position);
        float timeToHit = Mathf.Max(0.001f, distance / assumedBulletSpeed);
        Vector2 predictedPosition = (Vector2)currentTargetTransform.position + (targetVel * timeToHit);

        if (Time.time > timeToChangeOffset)
        {
            aimOffset = Random.insideUnitCircle * aimInaccuracy;
            timeToChangeOffset = Time.time + Random.Range(0.2f, 0.7f);
        }

        Vector2 targetPosition = predictedPosition + aimOffset;
        Vector2 directionToTarget = targetPosition - (Vector2)turretTransform.position;
        if (directionToTarget.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        turretTransform.rotation = Quaternion.RotateTowards(
            turretTransform.rotation,
            targetRotation,
            turretRotationSpeed * Time.deltaTime
        );
    }
    void MoveBase()
    {
        // Seguridad
        if (currentTargetTransform == null) return;

        // Reemplaza 'playerTransform' con 'currentTargetTransform'
        Vector2 directionToTarget = (currentTargetTransform.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, currentTargetTransform.position);
        if (distance > strafeDistance)
        {
            rb.velocity = directionToTarget * moveSpeed;
            RotateBaseTowards(directionToTarget);
        }
        else
        {
            if (Time.time > timeToChangeStrafe)
            {
                float action = Random.value;
                if (action < 0.4f) { strafeDirection = -1; isBackingUp = false; }
                else if (action < 0.8f) { strafeDirection = 1; isBackingUp = false; }
                else { isBackingUp = true; }
                timeToChangeStrafe = Time.time + Random.Range(0.5f, 1.5f);
            }
            if (isBackingUp)
            {
                rb.velocity = -directionToTarget * (moveSpeed * 0.7f);
                RotateBaseTowards(-directionToTarget);
            }
            else
            {
                Vector2 perpendicularDirection = new Vector2(-directionToTarget.y, directionToTarget.x) * strafeDirection;
                rb.velocity = perpendicularDirection * moveSpeed;
                RotateBaseTowards(perpendicularDirection);
            }
        }
    }

    void Wander()
    {
        // Girar el cañón hacia la base lentamente
        if (turretTransform != null && baseTransform != null)
        {
            turretTransform.rotation = Quaternion.RotateTowards(
                turretTransform.rotation,
                baseTransform.rotation,
                baseRotationSpeed * Time.deltaTime
            );
        }

        if (Vector2.Distance(transform.position, patrolTargetPosition) < 1.0f)
        {
            rb.velocity = Vector2.zero;
            isInvestigating = false; // Llegamos al punto
            if (Time.time >= timeToChangePatrolTarget)
            {
                SetNewPatrolTarget();
            }
        }
        else
        {
            if (Time.time >= stuckTimer)
            {
                SetNewPatrolTarget();
                return;
            }
            Vector2 directionToTarget = (patrolTargetPosition - (Vector2)transform.position).normalized;
            rb.velocity = directionToTarget * (moveSpeed * 0.5f);
            RotateBaseTowards(directionToTarget);
        }
    }

    void SetNewPatrolTarget()
    {
        patrolTargetPosition = (Vector2)transform.position + Random.insideUnitCircle * patrolRadius;
        currentPatrolWaitTime = Random.Range(patrolWaitTime.x, patrolWaitTime.y);
        timeToChangePatrolTarget = Time.time + currentPatrolWaitTime;
        stuckTimer = Time.time + patrolStuckTime;
    }

    void SetNewPatrolTarget(Vector2 targetPosition)
    {
        patrolTargetPosition = targetPosition;
        timeToChangePatrolTarget = Time.time;
        stuckTimer = Time.time + patrolStuckTime;
    }

    void RotateBaseTowards(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        baseTransform.rotation = Quaternion.RotateTowards(
            baseTransform.rotation,
            Quaternion.Euler(0, 0, targetAngle),
            baseRotationSpeed * Time.deltaTime
        );
    }

    void TryShoot()
    {
        if (!isPatrolling && Time.time >= nextFireTime)
        {
            if (isReloading) { return; }

            // Seguridad
            if (currentTargetTransform == null) return;

            // Reemplaza 'playerTransform' con 'currentTargetTransform'
            Vector2 dirToTarget = (currentTargetTransform.position - turretTransform.position).normalized;
            float angle = Vector2.Angle(turretTransform.up, dirToTarget);
            if (angle > firingAngleThreshold)
            {
                return;
            }
            if (currentAmmo > 0)
            {
                nextFireTime = Time.time + (1f / fireRate);
                Shoot();
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
    }

    void Shoot()
    {
        currentAmmo--;
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // --- ¡ESTA ES LA CORRECCIÓN! ---
                // Asigna el tag basado en el equipo de este tanque
                bulletScript.ownerTag = (team == AITeam.Enemy) ? "Enemy" : "Ally";
                // ---------------------------------
                
                int finalDamage = (int)(baseDamage * damageMultiplier);
                bulletScript.damageAmount = finalDamage;
                if (damageMultiplier > 1.0f)
                {
                    bulletScript.ActivateFireEffect();
                }
            }
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log(gameObject.name + " está recargando...");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magazineSize;
        isReloading = false;
        Debug.Log(gameObject.name + " recarga completa.");
    }

    // --- FUNCIONES DE BUFFS ---
    public void ApplySpeedBuff(float multiplier, float duration)
    {
        StartCoroutine(SpeedBuffCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBuffCoroutine(float multiplier, float duration)
    {
        if (originalSpeed <= 0) originalSpeed = moveSpeed;
        moveSpeed = originalSpeed * multiplier;
        if (motionTrail != null) motionTrail.StartTrail();
        yield return new WaitForSeconds(duration);
        moveSpeed = originalSpeed;
        originalSpeed = 0;
        if (motionTrail != null) motionTrail.StopTrail();
    }

    public void ApplyDamageBuff(float multiplier, float duration)
    {
        StartCoroutine(DamageBuffCoroutine(multiplier, duration));
    }

    private IEnumerator DamageBuffCoroutine(float multiplier, float duration)
    {
        damageMultiplier = multiplier;
        if (cannonFireEffect != null)
        {
            cannonFireEffect.SetActive(true);
        }
        yield return new WaitForSeconds(duration);
        damageMultiplier = 1.0f;
        if (cannonFireEffect != null)
        {
            cannonFireEffect.SetActive(false);
        }
    }
    
    public void OnTookDamage(Transform bulletTransform)
    {
        // 1. Si ya estamos en combate (no patrullando), ya sabemos dónde está el enemigo.
        if (!isPatrolling)
        {
            return;
        }

        // 2. Si estamos patrullando y nos disparan, nos "enfadamos"
        Debug.Log(gameObject.name + " fue golpeado, ¡investigando!");
        
        // 3. Calculamos la dirección de la que vino el disparo
        Vector2 attackDirection = (transform.position - bulletTransform.position).normalized;
        
        // 4. Decidimos un punto para investigar en esa dirección
        Vector2 investigationPoint = (Vector2)transform.position + (attackDirection * patrolRadius);

        // 5. Forzamos al estado de "Investigar"
        isInvestigating = true;
        SetNewPatrolTarget(investigationPoint); // Anula el patrullaje aleatorio
    }
}
