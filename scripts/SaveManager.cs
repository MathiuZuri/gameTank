using UnityEngine;
using System.IO;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;
using Firebase; 
using System; 

public class SaveManager : MonoBehaviour
{
    // Singleton
    public static SaveManager Instance { get; private set; }

    // Evento para avisar a la UI cuando los datos cambien
    public event Action OnDataLoaded;

    public PlayerData playerData;
    private string savePath;
    
    // Referencias a Firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private DocumentReference docRef;
    
    // Variable para saber si Firebase está listo
    public bool isFirebaseReady { get; private set; } = false;

    void Awake()
    {
        Debug.Log("RUTA DE GUARDADO: " + Application.persistentDataPath);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        UnityMainThreadDispatcher.Instance();
        
        savePath = Path.Combine(Application.persistentDataPath, "player.json");
        
        // 1. CARGA LOCAL INMEDIATA
        LoadGame();

        // 2. Iniciar Firebase en segundo plano
        InitializeFirebase();
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            try 
            {
                string json = File.ReadAllText(savePath);
                playerData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("Datos cargados desde LOCAL.");
            }
            catch
            {
                Debug.LogError("Archivo local corrupto. Creando nuevo.");
                playerData = new PlayerData();
                // Nombre random para local
                playerData.userName = "Jugador_" + UnityEngine.Random.Range(1000, 9999);
                SaveGame();
            }
        }
        else
        {
            Debug.LogWarning("No se encontró archivo de guardado. Creando uno nuevo.");
            playerData = new PlayerData();
            // Nombre random para local
            playerData.userName = "Jugador_" + UnityEngine.Random.Range(1000, 9999);
            SaveGame();
        }
        // Avisar a la UI que hay datos (aunque sean locales)
        OnDataLoaded?.Invoke();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                try
                {
                    // Crear App de forma segura
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    if (app == null) app = FirebaseApp.Create();

                    auth = FirebaseAuth.GetAuth(app);
                    db = FirebaseFirestore.GetInstance(app);
                    isFirebaseReady = true;
                    Debug.Log("Firebase inicializado correctamente.");
                    
                    // Si ya hay usuario, intentar bajar datos
                    if (auth.CurrentUser != null)
                    {
                        // Usamos el dispatcher para volver al hilo principal
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                             _ = LoadDataFromFirebase();
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error crítico Firebase: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("No se pudo conectar a Firebase: " + dependencyStatus);
            }
        });
    }

    // --- DESCARGAR DATOS (Mapeo Manual) ---
    public async Task LoadDataFromFirebase()
    {
        // 1. Sala de espera
        int intentos = 0;
        while (!isFirebaseReady && intentos < 30) 
        {
            await Task.Delay(100);
            intentos++;
        }
        
        if (!isFirebaseReady || auth.CurrentUser == null) return;

        Debug.Log("Cargando datos de nube para: " + auth.CurrentUser.UserId);
        
        if (db == null) db = FirebaseFirestore.DefaultInstance;
        docRef = db.Collection("players").Document(auth.CurrentUser.UserId);
        
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            var dict = snapshot.ToDictionary();
            
            // --- MAPEO DE DATOS SIMPLES ---
            // Usamos Convert.ToInt32 para evitar errores con Long de Firebase

            if(dict.ContainsKey("playerCoins")) 
                playerData.playerCoins = Convert.ToInt32(dict["playerCoins"]);
                
            if(dict.ContainsKey("userName")) 
                playerData.userName = dict["userName"].ToString();
                
            if(dict.ContainsKey("equippedTankID")) 
                playerData.equippedTankID = dict["equippedTankID"].ToString();

            if(dict.ContainsKey("selectedDifficultyID")) 
                playerData.selectedDifficultyID = dict["selectedDifficultyID"].ToString();

            if(dict.ContainsKey("selectedMapID")) 
                playerData.selectedMapID = dict["selectedMapID"].ToString();

            // --- MAPEO DE LISTAS (ARRAYS) ---

            // Lista de Tanques Desbloqueados
            if(dict.ContainsKey("unlockedTankIDs"))
            {
                var list = (List<object>)dict["unlockedTankIDs"];
                playerData.unlockedTankIDs = new List<string>(); // Limpiar antes de llenar
                foreach(var item in list) 
                {
                    playerData.unlockedTankIDs.Add(item.ToString());
                }
            }

            // Lista de Etiquetas de Usuario (si existe)
            if(dict.ContainsKey("userTag"))
            {
                // Nota: Si en la base de datos es un string simple, usa ToString()
                // Si es una lista, usa la lógica de arriba. Asumiré string simple por tu código previo.
                playerData.userTags = new List<string> { dict["userTag"].ToString() };
            }

            Debug.Log("¡Datos descargados y mapeados! Monedas: " + playerData.playerCoins);

            // 3. FINALIZAR
            SaveGame(); // Actualizar el archivo player.json local
            
            // Volver al hilo principal para avisar a la UI
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                OnDataLoaded?.Invoke();
            });
        }
        else
        {
            Debug.Log("Usuario nuevo en la nube. Creando perfil inicial...");
            // Si no existen datos en la nube, subimos los que tenemos en local
            await SaveDataToFirebase();
        }
    }

    // --- SUBIR DATOS (Mapeo Manual) ---
    public async Task SaveDataToFirebase()
    {
        if (!isFirebaseReady || auth.CurrentUser == null) return;
        if (auth.CurrentUser.IsAnonymous) return; // No guardar si es invitado

        if (db == null) db = FirebaseFirestore.DefaultInstance;
        docRef = db.Collection("players").Document(auth.CurrentUser.UserId);

        // 1. CREAR EL DICCIONARIO
        Dictionary<string, object> cloudData = new Dictionary<string, object>
        {
            { "playerCoins", playerData.playerCoins },
            { "equippedTankID", playerData.equippedTankID },
            { "selectedDifficultyID", playerData.selectedDifficultyID },
            { "selectedMapID", playerData.selectedMapID },
            { "userName", playerData.userName },
            { "userTag", playerData.userTags },
            { "unlockedTankIDs", playerData.unlockedTankIDs } 
        };

        // 2. ENVIAR
        await docRef.SetAsync(cloudData);
        Debug.Log("¡Datos guardados en Firebase (Mapeo Manual)!");
    }
    public void GiveCoins(int amount)
    {
        if (playerData == null) LoadGame();
        playerData.playerCoins += amount;
        Debug.Log("Monedas actuales: " + playerData.playerCoins);
        SaveGame(); 
    }
    
    public void EquipTank(string tankID)
    {
        if (playerData == null) LoadGame();
        if (playerData.unlockedTankIDs.Contains(tankID))
        {
            playerData.equippedTankID = tankID;
            SaveGame(); 
        }
    }
    
    public void SelectDifficulty(string difficultyID)
    {
        if (playerData == null) LoadGame();
        playerData.selectedDifficultyID = difficultyID;
        SaveGame(); 
    }
    
    public void SelectMap(string mapID)
    {
        if (playerData == null) LoadGame();
        playerData.selectedMapID = mapID;
        SaveGame(); 
    }
}