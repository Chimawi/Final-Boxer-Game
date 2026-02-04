using UnityEngine;
using UnityEngine.UI;


public class BrightnessLogic : MonoBehaviour
{
    public Slider slider;
    public Image panelBrillo;

    const string BRILLO_KEY = "brillo";

    void Start()
    {
        
        float value = PlayerPrefs.GetFloat(BRILLO_KEY, 0.5f);

       
        if (slider != null)
            slider.SetValueWithoutNotify(value);

        
        ApplyBrightness(value);
    }

    public void ChangeSlide(float valor)
    {
        PlayerPrefs.SetFloat(BRILLO_KEY, valor);
        PlayerPrefs.Save(); 

        ApplyBrightness(valor);
    }

    void ApplyBrightness(float value)
    {
        if (panelBrillo == null) return;

        Color c = panelBrillo.color;
        c.a = value;              
        panelBrillo.color = c;
    }
}
    

