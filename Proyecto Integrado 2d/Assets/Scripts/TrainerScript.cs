using UnityEngine;
using System.Collections;
using TMPro; 

[RequireComponent(typeof(AudioSource))]
public class NPCInteraction : MonoBehaviour
{
    [Header("Modo Intro")]
    public bool esIntroAutomatica = false; 

    [Header("UI")]
    public GameObject teclaEPrompt; 
    public GameObject panelDialogo;
    public TextMeshProUGUI textoDialogo;

    [Header("Diálogo")]
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
    private GameManager gameManager; // NUEVA REFERENCIA

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

        jugadorScript = FindFirstObjectByType<PlayerMovement>();
        enemigoScript = GetComponent<EnemyAI>();
        
        // Buscamos el GameManager automáticamente
        gameManager = FindFirstObjectByType<GameManager>();

        if (esIntroAutomatica)
        {
            EmpezarDialogo();
        }
        else
        {
            if(panelDialogo != null) panelDialogo.SetActive(false);
            if(teclaEPrompt != null) teclaEPrompt.SetActive(false);
        }
    }

    void Update()
    {
        bool pulsarBoton = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);

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
        if (lineasDelDialogo.Length == 0) return; 

        dialogoActivo = true;
        if(panelDialogo != null) panelDialogo.SetActive(true);
        if(teclaEPrompt != null) teclaEPrompt.SetActive(false);
        
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(true);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(true); 
        
        if (sonidoInicio != null) { audioSource.pitch = 1f; audioSource.PlayOneShot(sonidoInicio); }
        
        indiceFrase = 0;
        StartCoroutine(EscribirFrase());
    }

    void SiguienteFrase()
    {
        indiceFrase++;
        if (indiceFrase < lineasDelDialogo.Length) StartCoroutine(EscribirFrase());
        else TerminarDialogo();
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
                audioSource.pitch = Random.Range(0.9f, 1.1f); 
                audioSource.PlayOneShot(sonidosVoz[Random.Range(0, sonidosVoz.Length)]);
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

    // --- AQUÍ ESTÁ LA MAGIA ---
    void TerminarDialogo()
    {
        dialogoActivo = false;
        if(panelDialogo != null) panelDialogo.SetActive(false);
        isTyping = false;
        
        // CASO A: Es la Intro del Enemigo
        if (esIntroAutomatica)
        {
            esIntroAutomatica = false;
            
            // IMPORTANTE: NO descongelamos aquí. 
            // Llamamos al GameManager para que lance el "Ready? Fight!"
            if (gameManager != null)
            {
                gameManager.IniciarSecuenciaPelea();
            }
            else
            {
                // Si no hay GameManager, descongelamos por seguridad
                Debug.LogWarning("NPC: No encontré GameManager, descongelando manualmente.");
                DescongelarTodos();
            }
        }
        // CASO B: Es una charla normal con un NPC cualquiera
        else 
        {
            DescongelarTodos();
            if (jugadorCerca && teclaEPrompt != null) teclaEPrompt.SetActive(true);
        }
    }

    void DescongelarTodos()
    {
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(false);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(false);
    }
    // --------------------------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) { jugadorCerca = true; if (!dialogoActivo && !esIntroAutomatica && teclaEPrompt) teclaEPrompt.SetActive(true); }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) { jugadorCerca = false; if(teclaEPrompt) teclaEPrompt.SetActive(false); if (dialogoActivo) TerminarDialogo(); }
    }
}