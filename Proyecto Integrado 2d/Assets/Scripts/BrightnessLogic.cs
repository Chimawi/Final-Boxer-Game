using UnityEngine;
using UnityEngine.UI;

public class BrightnessLogic : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider slider;
    public Image panelBrillo; // Tu panel negro

    const string BRILLO_KEY = "brillo";

    void Start()
    {
        // Cargamos el valor guardado (por defecto 1 = brillo máximo/transparente)
        float value = PlayerPrefs.GetFloat(BRILLO_KEY, 1f); // Cambio default a 1

        // Si estamos en el Menú y hay slider, lo ajustamos
        if (slider != null)
        {
            slider.value = value;
        }

        // Aplicamos el brillo al panel
        ApplyBrightness(value);
    }


    // Esta función la llamas desde el Slider (On Value Changed)
    public void ChangeSlide(float valor)
    {
        Debug.Log("El slider se ha movido a: " + valor); // <--- NUEVO

        PlayerPrefs.SetFloat(BRILLO_KEY, valor);
        PlayerPrefs.Save();
        ApplyBrightness(valor);
    }

    void ApplyBrightness(float value)
    {
        if (panelBrillo == null) return;

        Color c = panelBrillo.color;

        // --- CAMBIA ESTA LÍNEA ---
        // Antes tenías: c.a = value;
        // Ahora pon esto:
        c.a = 1f - value;
        // -------------------------

        panelBrillo.color = c;
    }


}


