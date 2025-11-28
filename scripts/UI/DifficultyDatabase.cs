using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DifficultyDatabase", menuName = "Tanques/Difficulty Database")]
public class DifficultyDatabase : ScriptableObject
{
    public List<DifficultyLevel> allDifficulties;

    public DifficultyLevel GetDifficultyByID(string id)
    {
        foreach (DifficultyLevel difficulty in allDifficulties)
        {
            if (difficulty.difficultyID == id)
            {
                return difficulty;
            }
        }
        Debug.LogWarning("No se encontró una dificultad con el ID: " + id);
        return null;
    }
}