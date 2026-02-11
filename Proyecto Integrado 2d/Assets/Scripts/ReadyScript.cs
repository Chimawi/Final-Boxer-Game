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

    [Header("Sonidos (SFX)")]
    public AudioSource audioSource;
    public AudioClip vozReady; 
    public AudioClip vozFight;  
    public AudioClip sfxCampana;

    void Start()
    {
        // 1. Congelamos al inicio
        CongelarPersonajes(true);

        // 2. Si NO hay intro del enemigo, empezamos nosotros
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
        
        ReproducirSonido(vozReady);
        yield return new WaitForSeconds(esperaReady);

        // --- FASE 2: FIGHT!! ---
        if (textoCentro != null)
        {
            textoCentro.text = "Fight!!";
            textoCentro.color = Color.red; 
        }
        
        ReproducirSonido(vozFight);
        ReproducirSonido(sfxCampana);

        // --- FASE 3: ¡ACCIÓN! ---
        
        // 1. Quitamos el estado de diálogo
        CongelarPersonajes(false); 

        // 2. ¡IMPORTANTE! Activamos la variable de combate en los scripts
        // (Estas son las líneas que faltaban)
        if (playerScript != null) playerScript.combateIniciado = true;
        if (enemyScript != null) enemyScript.combateIniciado = true;
        if (enemyScript != null) enemyScript.IniciarCombate(); // Aseguramos activación extra

        Debug.Log("GameManager: ¡A PELEAR! Variables activadas.");

        yield return new WaitForSeconds(1.0f);
        if (textoCentro != null) textoCentro.text = "";
    }

    void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    void CongelarPersonajes(bool estado)
    {
        if (playerScript != null) playerScript.SetEstadoDialogo(estado);
        if (enemyScript != null) enemyScript.SetEstadoDialogo(estado);
    }
}