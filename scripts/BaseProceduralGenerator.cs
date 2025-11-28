using UnityEngine;

public abstract class BaseProceduralGenerator : MonoBehaviour
{
    [Header("UI Info")]
    [Tooltip("El ID único para este generador (ej: '__MAZE__')")]
    public string generatorID; // <-- ¡MUY IMPORTANTE!
    [Tooltip("El nombre que se muestra en el menú")]
    public string displayName;
    [Tooltip("La imagen de previsualización para el menú")]
    public Sprite icon;
    [Tooltip("Descripción para el menú de selección")]
    [TextArea(3, 5)] 
    public string description;
    
    public abstract int[,] GenerateMap(DifficultyLevel difficulty);
}