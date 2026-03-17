using UnityEngine;
using System.Collections;
using TMPro; 
using UnityEngine.UI; 

[RequireComponent(typeof(AudioSource))]
public class NPCInteraction : MonoBehaviour
{
    [Header("Modo Intro")]
    public bool esIntroAutomatica = false; 

    [Header("UI")]
    public GameObject teclaEPrompt; 
    public GameObject panelDialogo;
    public TextMeshProUGUI textoDialogo;

    [Header("UI - Guías y Progreso")]
    public GameObject indicadorPasarDialogo; 
    public GameObject flechaGuia;            
    public bool activarFlechaAlTerminar = true; 
    
    [Header("Opacidad y Parpadeo de la Flecha")]
    [Tooltip("La opacidad baja cuando la flecha no está activa (0 es invisible, 1 es sólida)")]
    public float faintedOpacity = 0.3f; 
    
    // NUEVO: Variables para controlar el tiempo del parpadeo
    [Tooltip("Tiempo en segundos que la flecha se ve totalmente sólida")]
    public float tiempoFlechaVisible = 0.4f;
    [Tooltip("Tiempo en segundos que la flecha se ve desvanecida (opacidad baja)")]
    public float tiempoFlechaDesvanecida = 0.2f;

    private Image flechaGuiaImageComponent; 
    private SpriteRenderer flechaGuiaSpriteComponent; 
    private Coroutine rutinaParpadeo;

    [Header("Animación del Botón E (Pulso)")]
    [Tooltip("Tiempo que tarda en completarse una fase (crecer o achicarse)")]
    public float animationDuration = 0.5f;
    public float maxScale = 1.2f; 
    private Coroutine rutinaAnimacionBoton;
    private RectTransform botonRectTransform; 
    private Vector3 escalaInicialBoton; 

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
    private GameManager gameManager; 

    private bool jugadorCerca;
    private bool dialogoActivo; 
    private int indiceFrase; 
    private bool isTyping; 
    
    private Coroutine rutinaEscribir; 

    void Start()
    {
        jugadorCerca = false;
        dialogoActivo = false;
        isTyping = false;
        audioSource = GetComponent<AudioSource>();

        jugadorScript = FindFirstObjectByType<PlayerMovement>();
        enemigoScript = GetComponent<EnemyAI>();
        gameManager = FindFirstObjectByType<GameManager>();

        if (indicadorPasarDialogo != null) indicadorPasarDialogo.SetActive(false);
        
        if (indicadorPasarDialogo != null)
        {
            botonRectTransform = indicadorPasarDialogo.GetComponent<RectTransform>();
            escalaInicialBoton = botonRectTransform.localScale; 
        }

        if (flechaGuia != null) 
        {
            flechaGuiaImageComponent = flechaGuia.GetComponentInChildren<Image>();
            flechaGuiaSpriteComponent = flechaGuia.GetComponentInChildren<SpriteRenderer>();
            
            flechaGuia.SetActive(false);
        }

        if (esIntroAutomatica) EmpezarDialogo();
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
        
        if (indicadorPasarDialogo != null) indicadorPasarDialogo.SetActive(true);
        
        if (indicadorPasarDialogo != null && botonRectTransform != null)
        {
            if (rutinaAnimacionBoton != null) StopCoroutine(rutinaAnimacionBoton);
            rutinaAnimacionBoton = StartCoroutine(AnimarBotonPulse());
        }

        if (flechaGuia != null) flechaGuia.SetActive(true);
        if (rutinaParpadeo != null) StopCoroutine(rutinaParpadeo);
        SetFlechaOpacity(faintedOpacity);
        
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(true);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(true); 
        
        if (sonidoInicio != null) { audioSource.pitch = 1f; audioSource.PlayOneShot(sonidoInicio); }
        
        indiceFrase = 0;
        if (rutinaEscribir != null) StopCoroutine(rutinaEscribir);
        rutinaEscribir = StartCoroutine(EscribirFrase());
    }

    void SiguienteFrase()
    {
        indiceFrase++;
        if (indiceFrase < lineasDelDialogo.Length) 
        {
            if (rutinaEscribir != null) StopCoroutine(rutinaEscribir);
            rutinaEscribir = StartCoroutine(EscribirFrase());
        }
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
        if (rutinaEscribir != null) StopCoroutine(rutinaEscribir); 
        textoDialogo.text = lineasDelDialogo[indiceFrase]; 
        isTyping = false; 
    }

    IEnumerator AnimarBotonPulse()
    {
        Vector3 initialScale = escalaInicialBoton; 
        Vector3 targetScale = escalaInicialBoton * maxScale; 

        float elapsedTime;

        while (true) 
        {
            elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                botonRectTransform.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / animationDuration);
                elapsedTime += Time.deltaTime;
                yield return null; 
            }
            botonRectTransform.localScale = targetScale; 

            elapsedTime = 0f;
            while (elapsedTime < animationDuration)
            {
                botonRectTransform.localScale = Vector3.Lerp(targetScale, initialScale, elapsedTime / animationDuration);
                elapsedTime += Time.deltaTime;
                yield return null; 
            }
            botonRectTransform.localScale = initialScale; 
        }
    }

    void TerminarDialogo()
    {
        dialogoActivo = false;
        if(panelDialogo != null) panelDialogo.SetActive(false);
        isTyping = false;
        
        if (indicadorPasarDialogo != null) indicadorPasarDialogo.SetActive(false);
        
        if (rutinaAnimacionBoton != null) StopCoroutine(rutinaAnimacionBoton);
        if (botonRectTransform != null) botonRectTransform.localScale = escalaInicialBoton;

        if (activarFlechaAlTerminar && flechaGuia != null)
        {
            flechaGuia.SetActive(true);
            if (rutinaParpadeo != null) StopCoroutine(rutinaParpadeo);
            rutinaParpadeo = StartCoroutine(ParpadearFlecha());
        }
        
        if (esIntroAutomatica)
        {
            esIntroAutomatica = false;
            
            if (gameManager != null) gameManager.IniciarSecuenciaPelea();
            else DescongelarTodos();
        }
        else 
        {
            DescongelarTodos();
            if (jugadorCerca && teclaEPrompt != null) teclaEPrompt.SetActive(true);
        }
    }

    // NUEVO: Ahora la corrutina usa las variables que configuraste en el Inspector
    IEnumerator ParpadearFlecha()
    {
        while (true)
        {
            SetFlechaOpacity(1f);
            yield return new WaitForSeconds(tiempoFlechaVisible); // Usa tu variable
            
            SetFlechaOpacity(faintedOpacity);
            yield return new WaitForSeconds(tiempoFlechaDesvanecida); // Usa tu variable
        }
    }

    private void SetFlechaOpacity(float opacity)
    {
        if (flechaGuiaImageComponent != null)
        {
            Color c = flechaGuiaImageComponent.color;
            c.a = opacity;
            flechaGuiaImageComponent.color = c;
        }
        else if (flechaGuiaSpriteComponent != null)
        {
            Color c = flechaGuiaSpriteComponent.color;
            c.a = opacity;
            flechaGuiaSpriteComponent.color = c;
        }
    }

    void DescongelarTodos()
    {
        if (jugadorScript != null) jugadorScript.SetEstadoDialogo(false);
        if (enemigoScript != null) enemigoScript.SetEstadoDialogo(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) { jugadorCerca = true; if (!dialogoActivo && !esIntroAutomatica && teclaEPrompt) teclaEPrompt.SetActive(true); }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) { jugadorCerca = false; if(teclaEPrompt) teclaEPrompt.SetActive(false); if (dialogoActivo) TerminarDialogo(); }
    }
}