using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    public PlayerData localData;
    public event Action OnDataUpdated;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    
    private string savePath;
    private string guestBackupPath;
    
    public bool isFirebaseReady { get; private set; } = false;
    public bool connectionFailed { get; private set; } = false;
    
    void Awake()

    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Inicializar el Dispatcher en el hilo principal

        UnityMainThreadDispatcher.Instance();
        savePath = Path.Combine(Application.persistentDataPath, "player.json");
        guestBackupPath = Path.Combine(Application.persistentDataPath, "guest_backup.json");
        // Cargar datos locales inmediatamente (Modo Offline por defecto)

        LoadLocal();
        // Intentar conectar servicios en segundo plano

        InitializeFirebase();

    }
    
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                isFirebaseReady = true; // <-- ¡Ahora está listo!
                Debug.Log("Firebase inicializado correctamente.");
                
                if (auth.CurrentUser != null)
                {
                    if (auth.CurrentUser.IsAnonymous)
                    {
                        // CASO 1: Limpiar anónimo viejo
                        Debug.Log("Limpiando sesión anónima antigua.");
                        auth.SignOut();
                    }
                    else
                    {
                        // CASO 2: Usuario real -> Cargar datos
                        Debug.Log("Sesión detectada. Sincronizando...");
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            _ = LoadFromCloud();
                        });
                    }
                }
            }
            else
            {
                Debug.LogError("No se pudo conectar a Firebase: " + dependencyStatus);
            }
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                OnDataUpdated?.Invoke();
            });
        });
    }
    
    void LoadLocal()
    {
        if (File.Exists(savePath))
        {
            try {
                localData = JsonUtility.FromJson<PlayerData>(File.ReadAllText(savePath));
            } catch {
                localData = new PlayerData();
            }
        }
        else
        {
            localData = new PlayerData();
            SaveLocal();
        }
        OnDataUpdated?.Invoke();
    }
    public void SaveLocal()
    {
        if (localData == null) localData = new PlayerData();
        File.WriteAllText(savePath, JsonUtility.ToJson(localData, true));
        // Si estamos conectados, intentamos subir a la nube también
        if (IsLoggedIn())
        {
            _ = SaveToCloud();
        }
    }
    private void BackupGuestData()
    {
        // Guardamos el estado actual antes de loguear para poder volver
        string json = JsonUtility.ToJson(localData, true);
        File.WriteAllText(guestBackupPath, json);
        Debug.Log("Backup local creado.");
    }
    public async void Login(string email, string pass, Action<string> callback)
    {
        if (!isFirebaseReady) { callback("Error de conexión"); return; }
        try
        {
            BackupGuestData(); // Guardar lo que tenemos antes de machacarlo
            await auth.SignInWithEmailAndPasswordAsync(email, pass);
            await LoadFromCloud(); // Bajar datos de la cuenta
            callback("SUCCESS");
        }
        catch (Exception e)
        {
            callback("Error: " + e.Message);
        }
    }
    public async void LinkAccount(string email, string pass, Action<string> callback)
    {
        if (!isFirebaseReady) { callback("Error de conexión"); return; }
        
        // Para vincular sin ser anónimo, primero creamos el usuario
        try
        {
            // En este enfoque simple, "Vincular" es básicamente "Registrar y Subir"
            await auth.CreateUserWithEmailAndPasswordAsync(email, pass);
            // Una vez creado y logueado, subimos nuestros datos locales a esta cuenta nueva
            await SaveToCloud();
            // Borramos el backup porque ahora estos datos SON la cuenta
            if (File.Exists(guestBackupPath)) File.Delete(guestBackupPath);
            callback("SUCCESS");

        }
        catch (Exception e)
        {
            callback("Error: " + e.Message);
        }

    }
    public void Logout()
    {
        if (!isFirebaseReady) return;
        if (auth.CurrentUser != null)
        {
            auth.SignOut();

        }
        // RESTAURAR DATOS LOCALES (BACKUP)
        if (File.Exists(guestBackupPath))

        {

            try {

                localData = JsonUtility.FromJson<PlayerData>(File.ReadAllText(guestBackupPath));

                Debug.Log("Datos offline restaurados.");

            } catch { 

                localData = new PlayerData(); 
            }
        }
        else
        {
            // Para ser seguros, mejor un invitado nuevo:
            localData = new PlayerData();
            localData.userName = "Invitado";
        }
        SaveLocal(); // Guardar el estado restaurado en player.json
        OnDataUpdated?.Invoke();
    }
    public async Task SaveToCloud()
    {
        if (!IsLoggedIn()) return; // Solo guardar si hay usuario real
        var dataMap = new Dictionary<string, object>
        {
            { "playerCoins", localData.playerCoins },
            { "userName", localData.userName },
            { "equippedTankID", localData.equippedTankID },
            { "unlockedTankIDs", localData.unlockedTankIDs },
            { "selectedDifficultyID", localData.selectedDifficultyID },
            { "selectedMapID", localData.selectedMapID },
            { "userTag", localData.userTags } 
        };
        await db.Collection("players").Document(auth.CurrentUser.UserId).SetAsync(dataMap);
    }
    public async Task LoadFromCloud()
    {
        if (!IsLoggedIn()) return;
        var doc = await db.Collection("players").Document(auth.CurrentUser.UserId).GetSnapshotAsync();
        if (doc.Exists)
        {
            var dict = doc.ToDictionary();
            // Mapeo
            if(dict.ContainsKey("playerCoins")) localData.playerCoins = Convert.ToInt32(dict["playerCoins"]);
            if(dict.ContainsKey("userName")) localData.userName = dict["userName"].ToString();
            if(dict.ContainsKey("equippedTankID")) localData.equippedTankID = dict["equippedTankID"].ToString();
            if(dict.ContainsKey("selectedDifficultyID")) localData.selectedDifficultyID = dict["selectedDifficultyID"].ToString();
            if(dict.ContainsKey("selectedMapID")) localData.selectedMapID = dict["selectedMapID"].ToString();
            if(dict.ContainsKey("unlockedTankIDs")) {
                var list = (List<object>)dict["unlockedTankIDs"];
                localData.unlockedTankIDs.Clear();
                foreach(var item in list) localData.unlockedTankIDs.Add(item.ToString());
            }
            if(dict.ContainsKey("userTag")) {
                 var list = (List<object>)dict["userTag"];
                 localData.userTags.Clear();
                 foreach(var item in list) localData.userTags.Add(item.ToString());
            }
            // (Pero NO sobrescribimos el backup guest_backup.json)
            File.WriteAllText(savePath, JsonUtility.ToJson(localData, true));
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                OnDataUpdated?.Invoke();
            });
        }
        else
        {
            // Usuario nuevo en nube -> Subir datos locales
            await SaveToCloud();
        }
    }
    // Helper para saber si estamos Online
    public bool IsLoggedIn()
    {
        return isFirebaseReady && auth != null && auth.CurrentUser != null;
    }
    public string GetUserID()
    {
        if (IsLoggedIn()) return auth.CurrentUser.UserId;
        return "Local (Sin conexión)";
    }
}