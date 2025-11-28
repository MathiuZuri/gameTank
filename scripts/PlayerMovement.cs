using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el 'Fixed Joystick' o 'Floating Joystick' de la izquierda.")]
    public Joystick joystick;
    
    [Tooltip("Arrastra aquí el objeto hijo 'BaseSprite'.")]
    public Transform baseTransform; 

    [Header("Configuración")]
    [Tooltip("Qué tan rápido se mueve el jugador.")]
    public float moveSpeed = 5.0f;

    // Variables Privadas
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    private float originalSpeed; // Para guardar la velocidad antes del buff
    
    private MotionTrail motionTrail; // <-- ¡AÑADE ESTO!

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalSpeed = moveSpeed; // Guarda la velocidad original al inicio
        if (joystick == null)
            Debug.LogError("¡No se ha asignado un Joystick al PlayerMovement!");
        if (baseTransform == null)
            Debug.LogError("¡No se ha asignado 'baseTransform' al PlayerMovement!");
        
        motionTrail = GetComponent<MotionTrail>();
    }

    void Update()
    {
        // 1. Leer la entrada (Esto no cambia)
        moveInput.x = joystick.Horizontal;
        moveInput.y = joystick.Vertical;
        
        // 2. Rotación de la base (Esto no cambia)
        if (moveInput.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            baseTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    // Se llama en el ciclo de física
    void FixedUpdate()
    {
        // 3. Aplicar el movimiento
        rb.velocity = moveInput.normalized * moveSpeed;
    }
    // Esta función la llamará el TankController
    public void InitializeSpeed()
    {
        originalSpeed = moveSpeed;
    }

    // ¡AÑADE ESTAS FUNCIONES!
    public void ApplySpeedBuff(float multiplier, float duration)
    {
        StartCoroutine(SpeedBuffCoroutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedBuffCoroutine(float multiplier, float duration)
    {
        // Si originalSpeed es 0, usar la velocidad actual como base
        if (originalSpeed <= 0) originalSpeed = moveSpeed;

        moveSpeed = originalSpeed * multiplier; // Aplica el buff
        if (motionTrail != null) motionTrail.StartTrail();

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed; // Vuelve a la normalidad
        originalSpeed = 0; // Resetea para el próximo buff

        if (motionTrail != null) motionTrail.StopTrail();
    }
}