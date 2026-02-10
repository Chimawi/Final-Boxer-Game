using UnityEngine;
using System.Collections;
using TMPro; 

[RequireComponent(typeof(AudioSource))]
public class NPCInteraction : MonoBehaviour
{
    [Header("Modo Intro")]
    [Tooltip("ACTIVAR ESTO EN EL ENEMIGO para que hable solo al iniciar.")]
    public bool esIntroAutomatica = false; 

    [Header("Configuración de UI")]
    public GameObject teclaEPrompt; 
    public GameObject panelDialogo;
    public TextMeshProUGUI textoDialogo;

    [Header("Configuración del Diálogo")]
    [TextArea(3, 10)]
    public string[] lineasDelDialogo;
    public float velocidadEscritura = 0.05f;

    [Header("Audio")]
    public AudioClip sonidoInicio; 
    public AudioClip[] sonidosVoz; 
    private AudioSource audioSource;

    // Referencias
    private PlayerMovement jugadorScript; 
    private EnemyAI enemigoScript; 

    // Estado interno
    private bool jugadorCerca;
    private bool dialogoActivo; 
    private int indiceFrase; 
    private bool isTyping; 

    void Start()
    {
        jugadorCerca = false;
        dialogoActivo = false;
        isTyping = false;
        audioSource = GetComponent<AudioSource>();

        // Buscamos los scripts
        jugadorScript = FindFirstObjectByType<PlayerMovement>();
        enemigoScript = GetComponent<EnemyAI>();

        // --- CORRECCIÓN AQUÍ ---
        if (esIntroAutomatica)
        {
            // Si es intro, NO ocultamos el panel. Lo encendemos directamente.
            EmpezarDialogo();
        }
        else
        {
            // Solo ocultamos la UI si NO es una intro automática
            if(panelDialogo != null) panelDialogo.SetActive(false);
            if(teclaEPrompt != null) teclaEPrompt.SetActive(false);
        }
    }

    void Update()
    {
        // Detectamos input (E o Click)
        bool pulsarBoton = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);

        // Permitimos avanzar si estamos cerca O si es la intro automática
        if ((jugadorCerca || esIntroAutomatica) && pulsarBoton)
        {
            if (!dialogoActivo)
            {
                if (!esIntroAutomatica) EmpezarDialogo();
            }
            else
            {
                if (isTyping) CompletarFraseActual();
                else SiguienteFrase();
            }
        }
    }

    void EmpezarDialogo()
    {
        // SEGURIDAD: Si se te olvidó escribir frases, avisamos y no hacemos nada
        if (lineasDelDialogo.Length == 0)
        {
            Debug.LogError("ERROR: El NPC no tiene frases en 'Lineas Del Dialogo'. Revisa el Inspector.");
            return; 
        }

        dialogoActivo = true;
        if(panelDialogo != null) panelDialogo.SetActive(true);
        if(teclaEPrompt != null) teclaEPrompt.SetActive(false);
        
        // Congelamos personajes
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(true);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(true); 
        
        // Sonido inicial
        if (sonidoInicio != null)
        {
            audioSource.pitch = 1f; 
            audioSource.PlayOneShot(sonidoInicio);
        }
        
        indiceFrase = 0;
        StartCoroutine(EscribirFrase());
    }

    void SiguienteFrase()
    {
        indiceFrase++;

        if (indiceFrase < lineasDelDialogo.Length)
        {
            StartCoroutine(EscribirFrase());
        }
        else
        {
            TerminarDialogo();
        }
    }

    IEnumerator EscribirFrase()
    {
        isTyping = true;
        textoDialogo.text = ""; 

        foreach (char letra in lineasDelDialogo[indiceFrase].ToCharArray())
        {
            textoDialogo.text += letra; 
            
            if (sonidosVoz.Length > 0 && letra != ' ')
            {
                int indiceRandom = Random.Range(0, sonidosVoz.Length);
                audioSource.pitch = Random.Range(0.9f, 1.1f); 
                audioSource.PlayOneShot(sonidosVoz[indiceRandom]);
            }

            yield return new WaitForSeconds(velocidadEscritura); 
        }

        isTyping = false; 
    }

    void CompletarFraseActual()
    {
        StopAllCoroutines(); 
        textoDialogo.text = lineasDelDialogo[indiceFrase]; 
        isTyping = false; 
    }

    void TerminarDialogo()
    {
        dialogoActivo = false;
        if(panelDialogo != null) panelDialogo.SetActive(false);
        isTyping = false;
        
        // Descongelamos
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(false);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(false);
        
        // Desactivamos el modo intro para que no se repita
        if (esIntroAutomatica)
        {
            esIntroAutomatica = false;
        }
        
        // Solo mostramos la E de nuevo si el jugador sigue cerca
        if (jugadorCerca && !esIntroAutomatica && teclaEPrompt != null)
        {
            teclaEPrompt.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (!dialogoActivo && !esIntroAutomatica && teclaEPrompt != null) 
                teclaEPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = false;
            if(teclaEPrompt != null) teclaEPrompt.SetActive(false);
            if (dialogoActivo) TerminarDialogo(); 
        }
    }
}