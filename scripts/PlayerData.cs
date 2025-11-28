using System.Collections.Generic;
using Firebase.Firestore;

// [System.Serializable] // <-- Esto era para Unity
[FirestoreData]

[System.Serializable]
public class PlayerData
{
// Datos de Juego
    public int playerCoins;
    public List<string> unlockedTankIDs;
    public string equippedTankID;
    public string selectedDifficultyID;
    public string selectedMapID;

    // Datos de Perfil
    public string userName;
    public List<string> userTags; // Lista para guardar varias etiquetas
    
    public PlayerData()
    {
        playerCoins = 0;
        unlockedTankIDs = new List<string>();
        // sea el ID EXACTO de tu tanque inicial en el TankData.
        string defaultTank = "t-45";
        unlockedTankIDs.Add("t-45"); // Dar el tanque inicial gratis
        equippedTankID = defaultTank;
        selectedDifficultyID = "facil";
        selectedMapID = "Maze1";
        
        userName = "Invitado";
        userTags = new List<string> { "Novato" };
    }
}