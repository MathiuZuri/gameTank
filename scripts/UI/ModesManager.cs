using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ModesManager : MonoBehaviour
{
    [Header("Dependencias")]
    public DifficultyDatabase difficultyDatabase; 

    [Header("UI - Lista")]
    public Transform listContent; 
    public GameObject modeButtonPrefab; 

    [Header("UI - Panel de Detalles")]
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;
    
    [Header("UI - Pop-ups")]
    [Tooltip("Arrastra aquí el panel 'Selección Exitosa'.")]
    public GameObject panelSeleccionExito;
    private DifficultyLevel currentSelectedDifficulty;

    void OnEnable()
    {
        
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame();
        RefreshList();
        
        // Limpiar el panel de detalles
        detailName.text = "Selecciona un modo";
        detailDescription.text = "";
        selectButton.interactable = false;
        selectButtonText.text = "Seleccionar";
        if (detailIcon != null) detailIcon.sprite = null; 

        // Asegurarse de que el pop-up esté oculto al inicio
        if (panelSeleccionExito != null)
        {
            panelSeleccionExito.SetActive(false);
        }
    }

    void RefreshList()
    {
        foreach (Transform child in listContent) { Destroy(child.gameObject); }
        foreach (DifficultyLevel difficulty in difficultyDatabase.allDifficulties)
        {
            GameObject itemGO = Instantiate(modeButtonPrefab, listContent);
            itemGO.GetComponent<ModeButton>().Setup(difficulty, SelectMode);
        }
    }

    // Esta función es llamada por el ModeButton
    public void SelectMode(DifficultyLevel data)
    {
        currentSelectedDifficulty = data;
        if (detailIcon != null) detailIcon.sprite = data.icon;
        if (detailName != null) detailName.text = data.difficultyName;
        if (detailDescription != null) detailDescription.text = data.description;

        if (SaveManager.Instance.playerData.selectedDifficultyID == data.difficultyID)
        {
            selectButton.interactable = false;
            selectButtonText.text = "Seleccionado";
        }
        else
        {
            selectButton.interactable = true;
            selectButtonText.text = "Seleccionar";
        }
    }
    
    public void OnSelectButtonPressed()
    {
        if (currentSelectedDifficulty == null) return;

        SaveManager.Instance.SelectDifficulty(currentSelectedDifficulty.difficultyID);
        
        selectButton.interactable = false;
        selectButtonText.text = "Seleccionado";
        if (panelSeleccionExito != null)
        {
            panelSeleccionExito.SetActive(true);
        }
    }
}