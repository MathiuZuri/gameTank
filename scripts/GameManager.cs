using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // <-- ¡NECESARIO PARA CAMBIAR DE ESCENA!
using UnityEngine.UI; 

public class GameManager : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public MapGenerator mapGenerator; // Arrastra el objeto con este script
    public Health playerHealth; // Arrastra el prefab del JUGADOR aquí
    public TankDatabase tankDatabase; // Arrastra tu GameTankDatabase.asset aquí
    public TankController playerTankController; // Arrastra tu Player aquí
    
    [Header("Referencias de UI (Arrastra desde la escena)")]
    public GameObject panelWin;
    public GameObject panelLose;
    public GameObject panelPause;

    // Control de Estado
    private List<TankAI> activeEnemies; // Lista de enemigos vivos
    private bool isGamePaused = false;
    private bool isGameOver = false;
    void Start()
    {
        // 1. Encontrar el SaveManager y cargar
        // saveManager = FindFirstObjectByType<SaveManager>(); // <-- ¡ELIMINA ESTA LÍNEA!
        if (SaveManager.Instance == null) // <-- ¡USA EL SINGLETON!
        {
            Debug.LogError("¡No se encontró el SaveManager!");
            return; // Añade un 'return' para seguridad
        }
        // 2. Configurar el jugador
        SetupPlayerTank();
        // 3. Suscribirse al evento de muerte del jugador
        if (playerHealth != null)
        {
            playerHealth.OnDie.AddListener(ShowLoseScreen);
        }
        else
        {
            Debug.LogError("¡No se asignó la 'Salud del Jugador' (playerHealth) en el GameManager!");
        }
        // 4. Lógica de Spawneo (Esto está perfecto)
        if (mapGenerator != null)
        {
            mapGenerator.GenerateLevel();
            mapGenerator.SpawnPlayer();
            mapGenerator.SpawnAllies();
            activeEnemies = mapGenerator.SpawnEnemies();
        }
    }
    void Update()
    {
        // Si el juego ha terminado, no hacer nada
        if (isGameOver) return;

        // Si el juego está en pausa, no revisar victoria/derrota
        if (isGamePaused) return;

        // Comprobar la condición de victoria
        CheckWinCondition();

        // Comprobar si el jugador quiere pausar
        if (Input.GetKeyDown(KeyCode.Escape)) // Puedes cambiarlo a un botón
        {
            TogglePause();
        }
    }
    void CheckWinCondition()
    {
        // Limpiamos la lista de enemigos que ya han sido destruidos (null)
        activeEnemies.RemoveAll(enemy => enemy == null);
        // Si no quedan enemigos, ¡ganamos!
        if (activeEnemies.Count == 0)
        {
            ShowWinScreen();
        }
    }
    public void ShowWinScreen()
    {
        if (isGameOver) return; // Evitar que se llame dos veces
        isGameOver = true;
        Time.timeScale = 0f; // Congelar el juego
        panelWin.SetActive(true);
    }
    public void ShowLoseScreen()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f; // Congelar el juego
        panelLose.SetActive(true);
    }
    
    public void TogglePause()
    {
        isGamePaused = !isGamePaused; // Invertir el estado de pausa
        if (isGamePaused)
        {
            Time.timeScale = 0f; // Congelar
            panelPause.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f; // Reanudar
            panelPause.SetActive(false);
        }
    }
    public async void ReturnToMenu()
    {
        // 1. Guardar los datos en la nube
        if (SaveManager.Instance != null)
        {
            await SaveManager.Instance.SaveDataToFirebase();
        }

        // 2. Reanudar el tiempo
        Time.timeScale = 1f;

        // 3. Cargar la escena
        SceneManager.LoadScene("Menu_inicio"); 
    }
    void SetupPlayerTank()
    {
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame(); 
        string equippedTankID = SaveManager.Instance.playerData.equippedTankID;
        if (equippedTankID == null || equippedTankID == "")
        {
            equippedTankID = "DefaultTank"; // Fallback
        }
        TankData dataToApply = tankDatabase.GetTankByID(equippedTankID);
        if (dataToApply != null && playerTankController != null)
        {
            playerTankController.ApplyTankData(dataToApply);
        }
    }
    
}