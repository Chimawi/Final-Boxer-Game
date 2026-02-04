using UnityEngine;
using UnityEngine.UI;

public class MusicVolume : MonoBehaviour
{
    public Slider slider;
    public float sliderValue;
    public Image imagenMute;

    private void Start()
    {
        slider.value = PlayerPrefs.GetFloat("volumenAudio", 0.5f);
        AudioListener.volume = slider.value;
        RevisarSiEstoyMute();
    }

    public void ChangeSlider(float valor)
    {
        sliderValue = valor;
        PlayerPrefs.SetFloat("volumenAudio", slider.value);
        AudioListener.volume = slider.value;
        RevisarSiEstoyMute();

    }

    public void RevisarSiEstoyMute()
    {
        if (slider.value == 0)
        {
            imagenMute.enabled = true;
        }
        else
        {
            imagenMute.enabled = false;
        }
    }
}
