using UnityEngine;
using UnityEngine.SceneManagement;

public class LoseAndWinManager : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("RingScene");
        Time.timeScale = 1f;
    }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
            Time.timeScale = 1f;
    }
}
