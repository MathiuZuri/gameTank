using UnityEngine;
using System.Collections; // Para Coroutines

public class MotionTrail : MonoBehaviour
{
    [Header("Configuración de la Estela")]
    [Tooltip("El Sprite Renderer del objeto principal que se va a clonar para la estela.")]
    public SpriteRenderer mainSpriteRenderer;
    [Tooltip("El prefab del 'Trail Sprite' (una copia del sprite principal con transparencia).")]
    public GameObject trailSpritePrefab; 
    [Tooltip("Cuán a menudo se deja una estela mientras se está activo.")]
    public float trailSpawnInterval = 0.05f;
    [Tooltip("Cuánto tiempo permanece visible cada parte de la estela.")]
    public float trailLifetime = 0.5f;
    [Tooltip("Qué tan transparente empieza la estela. Disminuye a 0 con el tiempo.")]
    [Range(0, 1)]
    public float startTransparency = 0.5f;

    private bool isTrailActive = false;
    private Coroutine currentTrailRoutine;
    public void StartTrail()
    {
        if (isTrailActive) return; // Ya está activo
        isTrailActive = true;
        currentTrailRoutine = StartCoroutine(SpawnTrailRoutine());
    }
    public void StopTrail()
    {
        if (!isTrailActive) return; // Ya está inactivo
        isTrailActive = false;
        if (currentTrailRoutine != null)
        {
            StopCoroutine(currentTrailRoutine);
        }
    }
    // Coroutine que genera las estelas
    private IEnumerator SpawnTrailRoutine()
    {
        while (isTrailActive)
        {
            if (trailSpritePrefab != null && mainSpriteRenderer != null)
            {
                // Instanciar el prefab de la estela
                GameObject trailPart = Instantiate(trailSpritePrefab, mainSpriteRenderer.transform.position, mainSpriteRenderer.transform.rotation);
                
                // Asegurarse de que el trail part tenga el mismo sprite y color actual
                SpriteRenderer trailRenderer = trailPart.GetComponent<SpriteRenderer>();
                if (trailRenderer != null)
                {
                    trailRenderer.sprite = mainSpriteRenderer.sprite;
                    trailRenderer.color = mainSpriteRenderer.color; // Mantiene el color actual (ej. amarillo de buff)
                    
                    // Asegurar que se dibuje detrás del personaje principal
                    trailRenderer.sortingOrder = mainSpriteRenderer.sortingOrder - 1; 
                }

                // Iniciar la rutina para desvanecer y destruir esta parte de la estela
                StartCoroutine(FadeAndDestroyTrail(trailRenderer, trailLifetime, startTransparency));
            }
            yield return new WaitForSeconds(trailSpawnInterval);
        }
    }

    // Coroutine que desvanece y destruye una parte de la estela
    private IEnumerator FadeAndDestroyTrail(SpriteRenderer sr, float life, float startAlpha)
    {
        float timer = 0f;
        Color startColor = sr.color;
        startColor.a = startAlpha; // Establece la transparencia inicial
        sr.color = startColor;

        while (timer < life)
        {
            timer += Time.deltaTime;
            float progress = timer / life;
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(startAlpha, 0f, progress); // Se desvanece a 0
            sr.color = currentColor;
            yield return null; // Esperar al siguiente frame
        }

        Destroy(sr.gameObject); // Destruir la parte de la estela
    }
}