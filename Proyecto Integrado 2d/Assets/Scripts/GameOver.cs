using UnityEngine;
using UnityEngine.SceneManagement;
public class GameOver : MonoBehaviour
{
    public GameObject gameOverPanel;

    public void ShowGameOver()
    {
        Time.timeScale = 0; // Detiene el tiempo del juego
        gameOverPanel.SetActive(true);
    }

    public void ReiniciarNivel()
    {
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void VolverAlMenu()
    {
        Time.timeScale = 1; // Asegura que el tiempo del juego se reanude
        SceneManager.LoadScene("MainMenu");
    }

}



