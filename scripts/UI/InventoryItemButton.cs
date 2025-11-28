using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; //

public class InventoryItemButton : MonoBehaviour
{
    public TankDisplayUI tankPreview;
    public TextMeshProUGUI tankNameText;
    public TextMeshProUGUI tankCostText;
    
    private TankData myTankData;
    // private ShopManager shopManager; // <-- REEMPLAZA ESTA LÍNEA...
    private Action<TankData> onClickCallback; // <-- ...CON ESTA LÍNEA

    public void Setup(TankData data, Action<TankData> onClickAction)
    {
        myTankData = data;
        onClickCallback = onClickAction; // Asigna la acción a llamar

        tankPreview.DisplayTank(data);
        tankNameText.text = data.tankName;

        // Ocultar el costo (en el inventario no se ve)
        if (tankCostText != null)
        {
            tankCostText.gameObject.SetActive(false);
        }
        
        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }
    public void OnItemClick()
    {
        // Llama a la función que nos pasaron (SelectTank de la Tienda o del Inventario)
        if (onClickCallback != null)
        {
            onClickCallback(myTankData);
        }
    }
}