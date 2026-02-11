using UnityEngine;
using UnityEngine.SceneManagement;

public class CambioDeEscena : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("El nombre de la escena final (ej: Nivel2)")]
    public string nombreEscenaDestino;

    [Tooltip("El nombre exacto de tu escena de carga (ej: PantallaCarga)")]
    public string nombreEscenaCarga = "PantallaCarga";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Guardamos en memoria a dónde queremos ir después
            PlayerPrefs.SetString("NivelDestino", nombreEscenaDestino);
            
            // 2. Cargamos la escena intermedia de carga
            SceneManager.LoadScene(nombreEscenaCarga);
        }
    }
}