using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MapButton : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    
    private Action onClickCallback; // Modificado

    // ¡Firma MODIFICADA!
    public void Setup(string displayName, Sprite displayIcon, Action onClickAction)
    {
        onClickCallback = onClickAction;

        if (iconImage != null) 
        {
            iconImage.sprite = displayIcon;
            iconImage.enabled = (displayIcon != null); // Oculta si no hay ícono
        }
        if (nameText != null) nameText.text = displayName;
        
        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }

    public void OnItemClick()
    {
        onClickCallback(); // Llama a la acción
    }
}