using UnityEngine;
using UnityEditor;
using System.IO;

public class ResetGameData
{
    [MenuItem("Herramientas/Borrar Datos de Jugador (Reset Total)")]
    public static void DeleteSaveFiles()
    {
        string path1 = Path.Combine(Application.persistentDataPath, "player.json");
        string path2 = Path.Combine(Application.persistentDataPath, "guest_backup.json");

        if (File.Exists(path1)) File.Delete(path1);
        if (File.Exists(path2)) File.Delete(path2);

        // También borramos PlayerPrefs por si acaso guardaste algo ahí
        PlayerPrefs.DeleteAll();

        Debug.Log("Todos los datos locales han sido eliminados! El juego empezará de 0.");
    }
}