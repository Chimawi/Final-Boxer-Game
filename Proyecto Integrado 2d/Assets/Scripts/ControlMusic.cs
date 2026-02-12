using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; // Necesario para controlar el Slider visualmente

public class ControlMusic : MonoBehaviour
{
    [Header("Referencias")]
    public AudioMixer audioMixer;
    public Slider sliderVisual; // Arrastra aquí tu slider del menú

    void Start()
    {
        // 1. Al iniciar el Menú, recuperamos el volumen guardado (por defecto 0.5)
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenMusicaGuardado", 0.5f);

        // 2. Ajustamos el slider visual a esa posición
        if (sliderVisual != null) sliderVisual.value = volumenGuardado;

        // 3. Aplicamos el volumen al mixer
        ControlDeMusica(volumenGuardado);
    }

    public void ControlDeMusica(float sliderMusica)
    {
        // NOTA IMPORTANTE: El slider no debe llegar a 0 absoluto, pon el Min Value en 0.0001
        // porque Log10 de 0 da error.

        // Calculamos el volumen logarítmico para el Mixer
        audioMixer.SetFloat("VolumenMusica", Mathf.Log10(sliderMusica) * 20);

        // Guardamos el valor para el futuro
        PlayerPrefs.SetFloat("VolumenMusicaGuardado", sliderMusica);
    }
}
