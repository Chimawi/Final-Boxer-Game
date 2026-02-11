using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;
    public GameObject puseButton;

    public void PauseGame()
    {
        Time.timeScale = 0; // Detiene el tiempo del juego
        puseButton.SetActive(false);
        pauseMenu.SetActive(true);
        
    }

    public void ResumeGame()
    {
        Time.timeScale = 1; // Reanuda el tiempo del juego
        puseButton.SetActive(true);
        pauseMenu.SetActive(false);
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1; // Asegura que el tiempo del juego se reanude
    }

    public void QuitGame()
        {
            Time.timeScale = 1; // Asegura que el tiempo del juego se reanude
            SceneManager.LoadScene("MainMenu");
    }
}
