using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class BotonSecreto : MonoBehaviour
{
    [Header("Configuración")]
    public int clicksNecesarios = 3;
    private int contadorClicks = 0;

    [Header("Referencias Video")]
    public VideoPlayer reproductorVideo;
    public RawImage pantallaDelVideo;

    [Header("Referencias Música")]
    public AudioSource musicaDeFondo; // --- NUEVO: Arrastra aquí el Audio Source que toca la música ---

    void Start()
    {
        // Al empezar, apagamos la pantalla del video
        if (pantallaDelVideo != null)
            pantallaDelVideo.gameObject.SetActive(false);

        // Preparamos el evento para cuando termine el video
        if (reproductorVideo != null)
            reproductorVideo.loopPointReached += AlTerminarVideo;
    }

    public void PresionarBoton()
    {
        contadorClicks++;

        if (contadorClicks >= clicksNecesarios)
        {
            EjecutarSecreto();
            contadorClicks = 0; // Reiniciamos el contador
        }
    }

    void EjecutarSecreto()
    {
        // 1. PAUSAMOS LA MÚSICA DE FONDO
        if (musicaDeFondo != null)
            musicaDeFondo.Pause();

        // 2. ENCENDEMOS LA PANTALLA Y VIDEO
        if (pantallaDelVideo != null)
        {
            pantallaDelVideo.gameObject.SetActive(true);

            if (reproductorVideo != null)
            {
                reproductorVideo.Stop();
                reproductorVideo.Play();
            }
        }
    }

    // Esta función salta sola al acabar el video
    void AlTerminarVideo(VideoPlayer vp)
    {
        // 3. APAGAMOS LA PANTALLA DEL VIDEO
        if (pantallaDelVideo != null)
            pantallaDelVideo.gameObject.SetActive(false);

        // 4. REANUDAMOS LA MÚSICA DONDE SE QUEDÓ
        if (musicaDeFondo != null)
            musicaDeFondo.UnPause();
    }
}