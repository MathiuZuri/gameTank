using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class MapManager : MonoBehaviour
{
    [Header("Dependencias")]
    public MapDatabase mapDatabase;
    
    [Header("Generadores Procedurales")]
    [Tooltip("Arrastra aquí TODOS tus objetos generadores (CaveGenerator_Holder, etc.)")]
    public List<BaseProceduralGenerator> proceduralGenerators;

    [Header("UI - Lista")]
    public Transform listContent; 
    public GameObject mapButtonPrefab; // Tu prefab 'ModeButton'

    [Header("UI - Panel de Detalles")]
    public Image detailPreviewImage;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;
    
    [Header("UI - Pop-ups")]
    public GameObject panelSeleccionExito; 

    // Guardamos el ID del mapa/generador seleccionado
    private string currentSelectedID; 

    void OnEnable()
    {
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.playerData == null) SaveManager.Instance.LoadGame();
        RefreshList();
        
        // Limpiar el panel de detalles
        detailName.text = "Selecciona un mapa";
        detailDescription.text = "";
        selectButton.interactable = false;
        selectButtonText.text = "Seleccionar";
        if (detailPreviewImage != null) detailPreviewImage.sprite = null; 
        if (panelSeleccionExito != null) panelSeleccionExito.SetActive(false);
    }
    void RefreshList()
    {
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }

        // 1. Añadir los Mapas de ASSETS (los 3 tuyos)
        foreach (MapData map in mapDatabase.allMaps)
        {
            GameObject itemGO = Instantiate(mapButtonPrefab, listContent);
            
            // Creamos una variable local para evitar problemas de closure
            MapData currentMap = map; 
            itemGO.GetComponent<MapButton>().Setup(
                currentMap.mapName, 
                currentMap.previewImage, 
                () => SelectMap(currentMap) // Llama a SelectMap
            );
        }

        // 2. Añadir los Mapas PROCEDURALES (tus 3 generadores)
        foreach (BaseProceduralGenerator generator in proceduralGenerators)
        {
            GameObject itemGO = Instantiate(mapButtonPrefab, listContent);
            
            BaseProceduralGenerator currentGen = generator;
            itemGO.GetComponent<MapButton>().Setup(
                currentGen.displayName, 
                currentGen.icon, 
                () => SelectProcedural(currentGen) // Llama a SelectProcedural
            );
        }
    }

    // Llamado al seleccionar un MAPA DE ASSET
    public void SelectMap(MapData data)
    {
        currentSelectedID = data.mapID; // Guarda el ID del asset

        // Actualizar el panel de detalles
        if (detailPreviewImage != null) detailPreviewImage.sprite = data.previewImage;
        if (detailName != null) detailName.text = data.mapName;
        if (detailDescription != null) detailDescription.text = data.description;

        CheckIfSelected();
    }
    // Llamado al seleccionar un MAPA PROCEDURAL
    public void SelectProcedural(BaseProceduralGenerator data)
    {
        currentSelectedID = data.generatorID; // Guarda el ID del generador (ej. "__CAVE__")

        // Actualizar el panel de detalles
        if (detailPreviewImage != null) detailPreviewImage.sprite = data.icon;
        if (detailName != null) detailName.text = data.displayName;
        if (detailDescription != null) detailDescription.text = data.description;

        CheckIfSelected();
    }

    // Función de ayuda para actualizar el botón
    void CheckIfSelected()
    {
        if (SaveManager.Instance.playerData.selectedMapID == currentSelectedID)
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
        if (string.IsNullOrEmpty(currentSelectedID)) return;

        // Guarda el ID que seleccionamos (sea de asset o procedural)
        SaveManager.Instance.SelectMap(currentSelectedID); 
        
        selectButton.interactable = false;
        selectButtonText.text = "Seleccionado";

        if (panelSeleccionExito != null)
        {
            panelSeleccionExito.SetActive(true);
        }
    }
}