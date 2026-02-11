using UnityEngine;
using System.Collections;
using TMPro; 

public class GameManager : MonoBehaviour
{
    [Header("Configuración de la Partida")]
    public bool esperarIntroEnemigo = true; 

    [Header("Protagonistas")]
    public PlayerMovement playerScript;
    public EnemyAI enemyScript;

    [Header("Interfaz (UI)")]
    public TextMeshProUGUI textoCentro; 
    public float esperaReady = 2.0f;   

    [Header("Canal 1: Efectos (SFX)")]
    public AudioSource audioSourceSFX; 
    public AudioClip vozReady; 
    public AudioClip vozFight;  
    public AudioClip sfxCampana;

    [Header("Canal 2: Música")]
    public AudioSource audioSourceMusica; 
    public AudioClip musicaPelea;
    [Range(0f, 1f)] public float volumenMusica = 0.5f;

    [Header("Canal 3: Ambiente (Público)")]
    public AudioSource audioSourceAmbiente; 
    public AudioClip sonidoPublico;         
    [Range(0f, 1f)] public float volumenAmbiente = 0.4f; 

    void Start()
    {
        CongelarPersonajes(true);

        if (!esperarIntroEnemigo)
        {
            IniciarSecuenciaPelea();
        }
    }

    public void IniciarSecuenciaPelea()
    {
        StartCoroutine(RutinaRound());
    }

    IEnumerator RutinaRound()
    {
        // --- FASE 1: READY? ---
        if (textoCentro != null)
        {
            textoCentro.text = "Are you ready?";
            textoCentro.color = Color.yellow;
        }
        
        ReproducirSFX(vozReady);
        yield return new WaitForSeconds(esperaReady);

        // --- FASE 2: FIGHT!! ---
        if (textoCentro != null)
        {
            textoCentro.text = "Fight!!";
            textoCentro.color = Color.red; 
        }
        
        ReproducirSFX(vozFight);
        ReproducirSFX(sfxCampana);

        // --- AQUI ACTIVAMOS EL LOOP (BUCLE) ---
        
        // 1. Música en Bucle
        if (audioSourceMusica != null && musicaPelea != null)
        {
            audioSourceMusica.clip = musicaPelea;
            audioSourceMusica.volume = volumenMusica;
            
            // ESTA LÍNEA HACE QUE SE REPITA SIEMPRE
            audioSourceMusica.loop = true; 
            
            audioSourceMusica.Play();
        }

        // 2. Público en Bucle
        if (audioSourceAmbiente != null && sonidoPublico != null)
        {
            audioSourceAmbiente.clip = sonidoPublico;
            audioSourceAmbiente.volume = volumenAmbiente;
            
            // ESTA LÍNEA HACE QUE SE REPITA SIEMPRE
            audioSourceAmbiente.loop = true; 
            
            audioSourceAmbiente.Play();
        }
        // -------------------------------------

        // --- FASE 3: ¡ACCIÓN! ---
        CongelarPersonajes(false); 

        if (playerScript != null) playerScript.combateIniciado = true;
        if (enemyScript != null) {
             enemyScript.combateIniciado = true;
             enemyScript.IniciarCombate();
        }

        yield return new WaitForSeconds(1.0f);
        if (textoCentro != null) textoCentro.text = "";
    }

    void ReproducirSFX(AudioClip clip)
    {
        if (audioSourceSFX != null && clip != null) audioSourceSFX.PlayOneShot(clip);
    }

    void CongelarPersonajes(bool estado)
    {
        if (playerScript != null) playerScript.SetEstadoDialogo(estado);
        if (enemyScript != null) enemyScript.SetEstadoDialogo(estado);
    }
}