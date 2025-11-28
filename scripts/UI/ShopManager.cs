using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Dependencias")]
    public TankDatabase tankDatabase;    

    [Header("UI - Lista")]
    public Transform shopListContent; 
    public GameObject shopItemPrefab; 

    [Header("UI - Panel de Detalles")]
    public TankDisplayUI detailPreview; 
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailCost;
    public Button buyButton;
    
    [Header("UI - Panel de Estadísticas")]
    [Tooltip("El 'StatsPanel' que contiene todos los textos de estadísticas.")]
    public GameObject statsPanel; // El panel padre
    public TextMeshProUGUI statHealth;
    public TextMeshProUGUI statSpeed;
    public TextMeshProUGUI statDamage;
    public TextMeshProUGUI statFireRate;
    public TextMeshProUGUI statMagazine;
    public TextMeshProUGUI statReload;
    public TextMeshProUGUI detailDescription;

    [Header("UI - Pop-ups")]
    public GameObject panelExito;
    public GameObject panelSinDinero;
    public GameObject panelYaComprado;

    [Header("UI - Dinero Jugador")]
    public TextMeshProUGUI playerCoinsText; 

    private TankData currentSelectedTank;

    // Esta función se llama cuando el panel de la tienda se activa
    void OnEnable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded += RefreshAll; // Suscribir

            if (SaveManager.Instance.playerData == null) 
                SaveManager.Instance.LoadGame();
        }

        // Refrescar inmediatamente al abrir
        RefreshAll();
        
        // Limpiar el panel de detalles y ocultar stats
        detailName.text = "Selecciona un tanque";
        detailCost.text = "";
        detailDescription.text = "";
        buyButton.interactable = false;
        if (statsPanel != null) statsPanel.SetActive(false); 
    }

    void OnDisable()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnDataLoaded -= RefreshAll; // Desuscribir
        }
    }
    void RefreshAll()
    {
        RefreshShopList();
        UpdatePlayerCoinsUI();
    }
    
    void UpdatePlayerCoinsUI()
    {
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.playerData != null)
        {
            playerCoinsText.text = "Dinero disponible: " + SaveManager.Instance.playerData.playerCoins;
        }
    }

    // Carga y muestra los tanques que el jugador NO posee
    void RefreshShopList()
    {
        // 1. Limpiar la lista antigua
        foreach (Transform child in shopListContent)
        {
            Destroy(child.gameObject);
        }

        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame();

        // 3. Generar la lista nueva
        foreach (TankData tank in tankDatabase.allTanks)
        {
            // ¡La lógica clave! Si el jugador NO lo tiene, lo muestra
            if (!SaveManager.Instance.playerData.unlockedTankIDs.Contains(tank.tankID))
            {
                
                GameObject itemGO = Instantiate(shopItemPrefab, shopListContent);
                itemGO.GetComponent<ShopItemButton>().Setup(tank, this);
            }
        }
    }

    // Esta función es llamada por el ShopItemButton
    public void SelectTank(TankData tank)
    {
        currentSelectedTank = tank;

        // Actualizar el panel de detalles
        detailPreview.DisplayTank(tank);
        detailName.text = tank.tankName;
        detailCost.text = "Costo: " + tank.shopPrice;
        detailDescription.text = tank.description;
        
        buyButton.interactable = true;
        
        // Activar el panel y rellenar las estadísticas
        if (statsPanel != null) statsPanel.SetActive(true);

        // Rellenar cada campo de texto con los datos del TankData
        if (statHealth != null) statHealth.text = "Vida: " + tank.maxHealth;
        if (statSpeed != null) statSpeed.text = "Velocidad: " + tank.moveSpeed;
        if (statDamage != null) statDamage.text = "Daño: " + tank.bulletDamage;
        if (statFireRate != null) statFireRate.text = "Cadencia: " + tank.fireRate + " /s";
        if (statMagazine != null) statMagazine.text = "Cargador: " + tank.magazineSize;
        if (statReload != null) statReload.text = "Recarga: " + tank.reloadTime + "s";
    }

    // Esta función la llama el botón "Comprar"
    public void OnBuyButtonPressed()
    {
        if (currentSelectedTank == null) return;

        // Comprobación 1: ¿Ya lo tenemos? (Doble chequeo)
        if (SaveManager.Instance.playerData.unlockedTankIDs.Contains(currentSelectedTank.tankID))
        {
            panelYaComprado.SetActive(true);
            return;
        }

        // Comprobación 2: ¿Tenemos dinero?
        if (SaveManager.Instance.playerData.playerCoins >= currentSelectedTank.shopPrice)
        {
            // ¡ÉXITO!
            SaveManager.Instance.playerData.playerCoins -= currentSelectedTank.shopPrice;
            SaveManager.Instance.playerData.unlockedTankIDs.Add(currentSelectedTank.tankID);
            SaveManager.Instance.SaveGame();

            panelExito.SetActive(true); // Mostrar pop-up de éxito
            RefreshShopList();          // Quitar el item de la tienda
            UpdatePlayerCoinsUI();      // Actualizar el dinero
            
            RefreshAll();
            // Desactivar el botón de compra de nuevo
            buyButton.interactable = false;
        }
        else
        {
            // ¡FALLO! No hay dinero
            panelSinDinero.SetActive(true);
        }
    }
}