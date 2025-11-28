using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UserPanelController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Dropdown tagsDropdown;

    [Header("Textos")]
    public TMP_Text userIdText;
    public TMP_Text statusText;

    [Header("Botones")]
    public Button btnLink;
    public Button btnLogin;
    public Button btnLogout;
    
    private bool lastReadyState = false;

    void OnEnable()
    {
        // Suscribirse a actualizaciones
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.OnDataUpdated += UpdateUI;
            UpdateUI(); // Actualizar al abrir
        }
    }

    void OnDisable()
    {
        if (GameNetworkManager.Instance != null)
            GameNetworkManager.Instance.OnDataUpdated -= UpdateUI;
    }
    
    // Verifica en cada frame si la conexión ya llegó
    void Update()
    {
        // Si no existe el manager, no hacemos nada
        if (GameNetworkManager.Instance == null) return;

        // Leemos el estado actual
        bool isReady = GameNetworkManager.Instance.isFirebaseReady;

        // Si el estado CAMBIÓ (de false a true), actualizamos la UI
        if (isReady != lastReadyState)
        {
            lastReadyState = isReady;
            Debug.Log("UI detectó cambio de estado en Firebase. Actualizando...");
            UpdateUI();
        }
        
        // OPCIONAL: Si la UI dice "Conectando..." pero Firebase ya está listo, forzar actualización
        if (isReady && statusText.text.Contains("Conectando"))
        {
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        var manager = GameNetworkManager.Instance;
        if (manager == null || manager.localData == null) return;
        if (manager.connectionFailed)
        {
            userIdText.text = "ID: Offline (Error de Red)";
        }
        else
        {
            userIdText.text = "ID: " + manager.GetUserID();
        }

        // 1. Actualizar textos
        if (nameInput) nameInput.text = manager.localData.userName;
        if (userIdText) userIdText.text = "ID: " + manager.GetUserID();

        // 2. Obtener estados
        bool isReady = manager.isFirebaseReady;
        bool isLoggedIn = manager.IsLoggedIn();
        bool isFailed = manager.connectionFailed;
        bool isGuest = !isLoggedIn; // Si no estamos logueados, somos invitados locales

        // 3. CONTROL DE BOTONES
        
        // Botón VINCULAR (Solo visible si eres invitado)
        if (btnLink) 
        {
            btnLink.gameObject.SetActive(isGuest); 
            btnLink.interactable = isReady; // Se desbloquea cuando Firebase carga
        }
        
        // Botón LOGIN (Solo visible si eres invitado)
        if (btnLogin) 
        {
            btnLogin.gameObject.SetActive(isGuest); 
            btnLogin.interactable = isReady; 
        }

        // Botón LOGOUT (Solo visible si estás logueado)
        if (btnLogout) 
        {
            btnLogout.gameObject.SetActive(isLoggedIn);
            // Siempre permitimos logout para poder arreglar bugs, 
            // pero idealmente debería ser 'isReady'
            btnLogout.interactable = true; 
        }

        // 4. Textos de estado
        if (statusText)
        {
            if (isFailed) statusText.text = "Error: No se pudo conectar a la nube.";
            else if (!isReady) statusText.text = "Conectando servicios...";
            else if (isLoggedIn) statusText.text = "Modo: Nube (" + manager.localData.userName + ")";
            else statusText.text = "Modo: Local (Invitado)";
        }
    }
    public void OnSaveNameChanged()
    {
        // Guardar nombre localmente al editar
        GameNetworkManager.Instance.localData.userName = nameInput.text;
        GameNetworkManager.Instance.SaveLocal();
    }

    public void OnClickLink()
    {
        Debug.Log("Botón Vincular presionado"); // <--- LOG DE DEBUG
        if (statusText) statusText.text = "Vinculando...";
        
        // Desactivar botones temporalmente para evitar doble click
        if(btnLink) btnLink.interactable = false;

        GameNetworkManager.Instance.LinkAccount(emailInput.text, passwordInput.text, (result) => {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                Debug.Log("Resultado Vincular: " + result);
                if (statusText) 
                {
                    if (result == "SUCCESS") statusText.text = "¡Vinculado! Verifica tu correo.";
                    else statusText.text = result;
                }
                // Llamar a UpdateUI reactivará los botones correctamente
                UpdateUI();
            });
        });
    }

    public void OnClickLogin()
    {
        Debug.Log("Botón Login presionado"); // <--- LOG DE DEBUG
        if (statusText) statusText.text = "Iniciando sesión...";
        
        if(btnLogin) btnLogin.interactable = false;

        GameNetworkManager.Instance.Login(emailInput.text, passwordInput.text, (result) => {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                Debug.Log("Resultado Login: " + result);
                if (statusText)
                {
                    if (result == "SUCCESS") statusText.text = "¡Datos descargados!";
                    else statusText.text = result;
                }
                UpdateUI();
            });
        });
    }

    public void OnClickLogout()
    {
        Debug.Log("Botón Logout presionado");
        GameNetworkManager.Instance.Logout();
        
        if (statusText) statusText.text = "Sesión cerrada.";
        UpdateUI();
    }
}