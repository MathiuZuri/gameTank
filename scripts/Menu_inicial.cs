using UnityEngine;
using UnityEngine.SceneManagement;
public class Menu_inicial : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
    }
}
