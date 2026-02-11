using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class LoadingManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("GarageScene");
    }
}
