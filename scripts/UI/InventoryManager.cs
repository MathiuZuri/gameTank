using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Dependencias")]
    public TankDatabase tankDatabase; // Arrastra tu GameTankDatabase.asset aquí

    [Header("UI - Lista")]
    public Transform inventoryListContent; // El 'Content' del Scroll View de tu inventario
    public GameObject itemButtonPrefab; // Tu prefab 'ShopItem_Button'

    [Header("UI - Panel de Detalles")]
    public TankDisplayUI detailPreview; // La previsualización grande
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public Button equipButton; // El botón "Equipar"
    public TextMeshProUGUI equipButtonText; // El texto del botón

    [Header("UI - Panel de Stats")]
    public GameObject statsPanel;
    public TextMeshProUGUI statHealth;
    public TextMeshProUGUI statSpeed;
    public TextMeshProUGUI statDamage;
    public TextMeshProUGUI statFireRate;
    public TextMeshProUGUI statMagazine;
    public TextMeshProUGUI statReload;

    [Header("UI - Pop-ups")]
    public GameObject panelSeleccionExito; // El panel "Selección Exitosa"
    private TankData _currentSelectedTank;
    // Se llama cuando se activa el panel de inventario
    void OnEnable()
    {
        // --- ¡Suscripción al evento! ---
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded += RefreshInventoryList; // Suscribir

            if (SaveManager.Instance.playerData == null) 
                SaveManager.Instance.LoadGame();
        }

        RefreshInventoryList();
        
        detailName.text = "Selecciona un tanque";
        detailDescription.text = "";
        equipButton.interactable = false;
        if (equipButtonText != null) equipButtonText.text = "Equipar";
        if (statsPanel != null) statsPanel.SetActive(false);
    }
    void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded -= RefreshInventoryList; // Desuscribir
        }
    }
    // Carga y muestra los tanques que el jugador SÍ posee
    void RefreshInventoryList()
    {
        foreach (Transform child in inventoryListContent)
        {
            Destroy(child.gameObject);
        }

        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame();

        // 3. Generar la lista nueva
        foreach (TankData tank in tankDatabase.allTanks)
        {
            // ¡La lógica INVERSA a la tienda!
            if (SaveManager.Instance.playerData.unlockedTankIDs.Contains(tank.tankID))
            {
                GameObject itemGo = Instantiate(itemButtonPrefab, inventoryListContent);
                itemGo.GetComponent<InventoryItemButton>().Setup(tank, SelectTank);
            }
        }
    }
    // Esta función es llamada por el ShopItemButton
    public void SelectTank(TankData tank)
    {
        _currentSelectedTank = tank;
        // Actualizar el panel de detalles
        detailPreview.DisplayTank(tank);
        detailName.text = tank.tankName;
        detailDescription.text = tank.description;

        // Mostrar estadísticas
        if (statsPanel != null) statsPanel.SetActive(true);
        if (statHealth != null) statHealth.text = "Vida: " + tank.maxHealth;
        if (statSpeed != null) statSpeed.text = "Velocidad: " + tank.moveSpeed;
        if (statDamage != null) statDamage.text = "Daño: " + tank.bulletDamage;
        if (statFireRate != null) statFireRate.text = "Cadencia: " + tank.fireRate + " /s";
        if (statMagazine != null) statMagazine.text = "Cargador: " + tank.magazineSize;
        if (statReload != null) statReload.text = "Recarga: " + tank.reloadTime + "s";

        // Actualizar el botón
        if (SaveManager.Instance.playerData.equippedTankID == tank.tankID)
        {
            // Si ya está equipado
            equipButton.interactable = false;
            if (equipButtonText != null) equipButtonText.text = "Equipado";
        }
        else
        {
            // Si no está equipado
            equipButton.interactable = true;
            if (equipButtonText != null) equipButtonText.text = "Equipar";
        }
    }

    // Esta función la llama el botón "Equipar"
    public void OnEquipButtonPressed()
    {
        if (_currentSelectedTank == null) return;

        // Llama al SaveManager para equipar
        SaveManager.Instance.EquipTank(_currentSelectedTank.tankID);

        // Muestra el pop-up de éxito
        panelSeleccionExito.SetActive(true);

        // Actualiza el botón para que diga "Equipado"
        equipButton.interactable = false;
        if (equipButtonText != null) equipButtonText.text = "Equipado";
    }
}