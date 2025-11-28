using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Tanques/Difficulty Level")]
public class DifficultyLevel : ScriptableObject
{
    [Header("Configuración de Nivel")]
    public string difficultyID; // ej: "Facil", "Dificil"
    public string difficultyName; // ej: "Modo Fácil"
    public Sprite icon; // Un ícono para la lista
    public int numberOfEnemiesToSpawn = 10;

    [TextArea(3, 5)]
    public string description; // Descripción para el panel de detalles
    
    [Header("Tipos de Tanques (Enemigos)")]
    [Tooltip("Arrastra aquí TODOS los 'TankData' (assets) que pueden aparecer en esta dificultad.")]
    public List<TankData> enemyTankTypes;
    
    [Header("Configuración de Aliados")]
    public int numberOfAlliesToSpawn = 2;
    [Tooltip("Arrastra aquí TODOS los 'TankData' (assets) que usarán los aliados.")]
    public List<TankData> allyTankTypes;
}