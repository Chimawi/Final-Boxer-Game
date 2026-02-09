using UnityEngine;
using UnityEngine.UI;

public class BarraDeVida : MonoBehaviour
{
    public Image barraDeVida;

    // Quitamos Update para ahorrar rendimiento. 
    // La barra solo cambiará cuando "alguien" se lo ordene.

    public void InicializarBarra(float cantidadVida)
    {
        // Al empezar, llenamos la barra al máximo
        barraDeVida.fillAmount = 1f;
    }

    public void CambiarVidaActual(float vidaActual, float vidaMaxima)
    {
        // Calculamos el porcentaje (Ej: 80 / 100 = 0.8)
        barraDeVida.fillAmount = vidaActual / vidaMaxima;
    }
}