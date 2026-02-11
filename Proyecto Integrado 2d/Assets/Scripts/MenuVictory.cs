using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class MenuVictoria : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre exacto de tu escena del Menú Principal")]
    public string nombreEscenaMenu = "MenuPrincipal";

    [Tooltip("Nombre exacto del siguiente nivel (Ej: Nivel2)")]
    public string nombreSiguienteNivel;

    [Tooltip("Nombre de tu escena de carga (Debe ser igual al que usaste antes)")]
    public string nombreEscenaCarga = "PantallaCarga";

    // --- FUNCIÓN PARA EL BOTÓN 'NEXT LEVEL' ---
    public void IrSiguienteNivel()
    {
        // 1. Nos aseguramos de que el tiempo corra (por si el juego estaba pausado)
        Time.timeScale = 1f;

        // 2. Guardamos en la memoria a dónde queremos ir
        PlayerPrefs.SetString("NivelDestino", nombreSiguienteNivel);

        // 3. Cargamos la pantalla de carga (ella se encargará de llevarnos al nivel)
        SceneManager.LoadScene(nombreEscenaCarga);
    }

    // --- FUNCIÓN PARA EL BOTÓN 'EXIT' (MAIN MENU) ---
    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        // Cargamos el menú directamente (suele ser ligero y no necesita carga)
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}
