using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ModeButton : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;

    private DifficultyLevel myDifficulty;
    private Action<DifficultyLevel> onClickCallback;

    public void Setup(DifficultyLevel data, Action<DifficultyLevel> onClickAction)
    {
        myDifficulty = data;
        onClickCallback = onClickAction;

        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.difficultyName;

        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }

    public void OnItemClick()
    {
        onClickCallback(myDifficulty);
    }
}