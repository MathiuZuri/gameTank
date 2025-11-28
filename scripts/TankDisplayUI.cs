using UnityEngine;
using UnityEngine.UI;

public class TankDisplayUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Arrastra aquí la imagen de la Base del tanque.")]
    public Image baseImage;

    [Tooltip("Arrastra aquí la imagen de la Torreta del tanque.")]
    public Image turretImage;

    // Esta es la función principal que llamarás
    public void DisplayTank(TankData tankToShow)
    {
        if (tankToShow == null)
        {
            Debug.LogError("No se pasó ningún TankData para mostrar.");
            return;
        }
        // Asigna los sprites del ScriptableObject a las imágenes de la UI
        if (baseImage != null)
        {
            // Antes: baseImage.sprite = tankToShow.baseSprite;
            baseImage.sprite = tankToShow.playerVisuals.baseSprite; // <-- LÍNEA MODIFICADA
        }

        if (turretImage != null)
        {
            turretImage.sprite = tankToShow.playerVisuals.turretSprite; // <-- LÍNEA MODIFICADA
        }
    }
}