using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButton : MonoBehaviour
{
    public TankDisplayUI tankPreview; // Arrastra tu prefab TankPreviewSlot aquí
    public TextMeshProUGUI tankNameText;

    private TankData myTankData;
    private ShopManager shopManager;

    // El ShopManager usa esto para configurar el botón
    public void Setup(TankData data, ShopManager manager)
    {
        myTankData = data;
        shopManager = manager;

        tankPreview.DisplayTank(data);
        tankNameText.text = data.tankName;

        // Asigna la función OnClick desde el código
        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }
    // Cuando se hace clic en este botón, le dice al manager qué tanque mostrar
    public void OnItemClick()
    {
        shopManager.SelectTank(myTankData);
    }
}