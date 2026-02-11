using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    // ==========================================
    // 1. CONFIGURACIÓN Y ESTADÍSTICAS
    // ==========================================
    [Header("Estadísticas Vitales")]
    public float vida = 100f;
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f; 
    public float tiempoAturdimiento = 0.5f; 

    [Header("IA - Inteligencia de Combate")]
    public float rangoAtaque = 0.8f; 
    public float cooldownAcciones = 3.0f;
    private float tiempoSiguienteAccion = 0f;
    
    [Tooltip("Arrastra aquí el objeto hijo (Pecho/Ojos) para medir distancia real")]
    public Transform puntoDeVision; 

    [Header("UI - Barras")]
    public BarraDeVida barraDeVidaScript; 

    [Header("Audio Combate (Arrays)")]
    [Tooltip("Arrastra aquí 2 o 3 sonidos de 'Aire/Whoosh'")]
    public AudioClip[] sfxLanzarGolpe; 
    [Tooltip("Arrastra aquí 2 o 3 sonidos de 'Impacto/Golpe Fuerte'")]
    public AudioClip[] sfxImpacto;     
    private AudioSource audioSource;

    [Header("Configuración de Ataque")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask layerJugador; 
    
    [Header("Movimiento")]
    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f; // Cada cuánto cambia de dirección al patrullar

    // ==========================================
    // 2. VARIABLES INTERNAS Y ESTADOS
    // ==========================================
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer[] partesDelCuerpo; 
    private Transform playerTransform; 

    // Estados de Control
    public bool combateIniciado = false; // Controlado por GameManager
    public bool enDialogo = false;       // Controlado por NPCInteraction
    
    // Estados de Acción
    private bool estaAturdido = false; 
    private bool estaContraatacando = false; 
    public bool jugadorDetectado = false; 
    private bool celebrandoVictoria = false; 

    private float tiempoTranscurrido;
    private int direccionMovimiento = 1;

    // ==========================================
    // 3. CICLO DE VIDA (Awake & Update)
    // ==========================================
    void Awake()
    {
        // Inicializamos referencias antes que nadie (Evita NullReference)
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>(); 
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        
        // Importante: Empezamos congelados
        combateIniciado = false; 
        
        ElegirNuevaDireccionAleatoria();
    }

    void Update()
    {
        // CANDADO MAESTRO: Si pasa cualquiera de esto, el enemigo NO piensa ni se mueve.
        if (!combateIniciado || vida <= 0 || estaAturdido || estaContraatacando || celebrandoVictoria || enDialogo) return;
        
        // Lógica de IA
        if (jugadorDetectado && playerTransform != null)
        {
            ComportamientoCombate();
        }
        else
        {
            Patrullar();
        }
    }

    // ==========================================
    // 4. CONTROL EXTERNO (GameManager / Diálogo)
    // ==========================================
    
    // Llamado por GameManager cuando termina la cuenta atrás
    public void IniciarCombate()
    {
        combateIniciado = true;
        // Pequeño reset para que no ataque en el milisegundo 0
        tiempoSiguienteAccion = Time.time + 1.0f;
    }

    // Llamado por NPCInteraction durante la intro
    public void SetEstadoDialogo(bool estado)
    {
        enDialogo = estado;

        if (enDialogo)
        {
            // Frenado total
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Block");
            StopAllCoroutines(); 
        }
        else
        {
            // Al salir del diálogo, reactivamos patrulla
            ElegirNuevaDireccionAleatoria();
        }
    }

    // ==========================================
    // 5. INTELIGENCIA ARTIFICIAL (IA)
    // ==========================================
    void ComportamientoCombate()
    {
        // Calculamos distancia
        Vector2 miPosicion = (puntoDeVision != null) ? puntoDeVision.position : transform.position;
        Vector2 posicionJugador = playerTransform.position;
        
        // Ajuste para detectar el centro del collider del jugador
        Collider2D colliderJugador = playerTransform.GetComponent<Collider2D>();
        if (colliderJugador != null) posicionJugador = colliderJugador.bounds.center;

        float distancia = Vector2.Distance(miPosicion, posicionJugador);
        
        if (distancia <= rangoAtaque)
        {
            // RANGO DE ATAQUE: Nos quedamos quietos y decidimos
            rb.linearVelocity = Vector2.zero;
            DetenerAnimacionesMovimiento();
            
            // Miramos siempre al jugador
            direccionMovimiento = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
            // (Aquí podrías rotar el sprite si usas FlipX, pero tu animador usa BlendTrees probablemente)

            if (Time.time >= tiempoSiguienteAccion)
            {
                DecidirAccion();
            }
        }
        else
        {
            // RANGO DE PERSECUCIÓN
            PerseguirJugador();
        }
    }

    void DecidirAccion()
    {
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
        
        // 70% Probabilidad de Atacar, 30% Bloquear
        if (Random.value > 0.3f) 
        {
            animator.SetTrigger("Attack");
            // AUDIO: Sonido de esfuerzo/aire al iniciar el golpe
            ReproducirSonido(sfxLanzarGolpe);
        }
        else 
        {
            animator.SetTrigger("Block");
        }
    }

    void PerseguirJugador()
    {
        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
        direccionMovimiento = (int)dirX;
        
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidad, rb.linearVelocity.y);
        ActualizarAnimacionesMovimiento();
    }

    void Patrullar()
    {
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidad, rb.linearVelocity.y);
        ActualizarAnimacionesMovimiento();
        
        tiempoTranscurrido += Time.deltaTime;
        if (tiempoTranscurrido >= intervaloCambio)
        {
            ElegirNuevaDireccionAleatoria();
            tiempoTranscurrido = 0;
        }
    }

    // ==========================================
    // 6. SISTEMA DE COMBATE Y FÍSICAS
    // ==========================================

    // Llamado por evento de Animación en el frame del golpe
    public void DetectarGolpe()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, layerJugador);
        bool golpeAcertado = false;

        foreach (Collider2D hit in hits)
        {
            if (hit is BoxCollider2D) continue; // Ignoramos triggers si es necesario

            PlayerMovement player = hit.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Vector2 direccionEmpuje = (player.transform.position - transform.position).normalized;
                player.RecibirDaño(1f, direccionEmpuje, this); // 'this' pasa la referencia del enemigo
                golpeAcertado = true;
            }
        }

        // AUDIO: Si impactamos, suena el golpe fuerte (superpuesto al anterior)
        if (golpeAcertado)
        {
            ReproducirSonido(sfxImpacto);
        }
    }

    // Función auxiliar para gestionar el audio
    void ReproducirSonido(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && audioSource != null)
        {
            AudioClip clipElegido = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Variación de tono humana
            audioSource.PlayOneShot(clipElegido); // Permite solapamiento de sonidos
        }
    }

    // Llamado cuando el jugador nos pega a nosotros
    public void RecibirDaño(float daño, Vector2 direccionEmpuje, PlayerMovement atacante = null)
    {
        if (vida <= 0) return;

        // Si estamos bloqueando...
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
            Debug.Log("Enemigo: ¡Bloqueado!");
            if (atacante != null) atacante.RecibirAturdimientoPorBloqueo();
            
            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            
            // Iniciamos contraataque
            float direccionHaciaJugador = -Mathf.Sign(direccionEmpuje.x);
            StartCoroutine(RutinaContraataque(direccionHaciaJugador));
            return;
        }

        // Si nos entra el golpe...
        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);

        StartCoroutine(RutinaEmpuje(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red)); 
    }

    public void RecibirAturdimientoPorBloqueo()
    {
        if (vida > 0) StartCoroutine(RutinaStunAmarillo());
    }

    // ==========================================
    // 7. CORRUTINAS (Animaciones temporales)
    // ==========================================

    IEnumerator RutinaContraataque(float dirX)
    {
        estaContraatacando = true; 
        yield return new WaitForSeconds(0.2f); // Pequeña pausa dramática tras bloquear

        // 1. Dash hacia el jugador
        float velocidadAtaque = velocidad * 2f; 
        rb.linearVelocity = new Vector2(dirX * velocidadAtaque, rb.linearVelocity.y);

        if (dirX > 0) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); } 
        else { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); }

        yield return new WaitForSeconds(0.4f);

        // 2. Frenar y Golpear
        rb.linearVelocity = Vector2.zero;
        DetenerAnimacionesMovimiento(); 
        
        animator.ResetTrigger("Block"); 
        animator.SetTrigger("Attack");
        
        // AUDIO: Sonido de ataque en el contraataque
        ReproducirSonido(sfxLanzarGolpe);

        yield return new WaitForSeconds(0.8f); // Tiempo que dura la animación de ataque
        estaContraatacando = false; 
        
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
    }

    IEnumerator RutinaStunAmarillo()
    {
        estaAturdido = true;
        animator.SetTrigger("Hurt"); 
        rb.linearVelocity = Vector2.zero; 
        StartCoroutine(EfectoParpadeo(Color.yellow));
        yield return new WaitForSeconds(tiempoAturdimiento + 0.5f); 
        estaAturdido = false;
        rb.linearVelocity = Vector2.zero; 
    }

    IEnumerator EfectoBloqueo(Vector2 direccion)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 empujeSuave = new Vector2(direccion.x, 0).normalized;
        rb.AddForce(empujeSuave * (fuerzaEmpuje / 2), ForceMode2D.Impulse);
        yield return null;
    }

    IEnumerator RutinaEmpuje(Vector2 direccion)
    {
        estaAturdido = true;
        animator.SetTrigger("Hurt"); 
        rb.linearVelocity = Vector2.zero; 
        Vector2 empujeHorizontal = new Vector2(direccion.x, 0).normalized; 
        rb.AddForce(empujeHorizontal * fuerzaEmpuje, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(tiempoAturdimiento);
        
        if (vida <= 0) Morir();
        else { estaAturdido = false; rb.linearVelocity = Vector2.zero; }
    }
    
    IEnumerator EfectoParpadeo(Color colorObjetivo)
    {
        Color colorNormal = Color.white; 
        for (int i = 0; i < 3; i++)
        {
            foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorObjetivo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorNormal;
    }

    IEnumerator RutinaVictoriaEnemigo()
    {
        yield return new WaitForSeconds(2.0f); 
        animator.SetTrigger("Victory");
    }

    // ==========================================
    // 8. UTILIDADES
    // ==========================================

    void Morir()
    {
        animator.SetTrigger("Die");
        estaAturdido = true; 
        
        GetComponent<Collider2D>().enabled = false; 
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>(); 
        if (player != null) player.ActivarVictoria();
        
        this.enabled = false; 
    }

    public void ActivarVictoria()
    {
        if (celebrandoVictoria) return;
        celebrandoVictoria = true; 

        StopAllCoroutines(); 
        rb.linearVelocity = Vector2.zero;
        DetenerAnimacionesMovimiento();
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Block");

        StartCoroutine(RutinaVictoriaEnemigo());
    }

    void ElegirNuevaDireccionAleatoria()
    {
        int aleatorio = Random.Range(0, 2);
        direccionMovimiento = (aleatorio == 0) ? -1 : 1;
        ActualizarAnimacionesMovimiento();
    }

    void ActualizarAnimacionesMovimiento() 
    { 
        if (direccionMovimiento == 1) { 
            animator.SetBool("IsWalking", false); 
            animator.SetBool("IsBackWalking", true); 
        } else { 
            animator.SetBool("IsWalking", true); 
            animator.SetBool("IsBackWalking", false); 
        } 
    }
    
    void DetenerAnimacionesMovimiento() 
    { 
        animator.SetBool("IsWalking", false); 
        animator.SetBool("IsBackWalking", false); 
    }

    // Detección de Trigger (Visión)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            jugadorDetectado = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorDetectado = false;
            playerTransform = null;
        }
    }

    // Gizmos para ver los rangos en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroGizmo = (puntoDeVision != null) ? puntoDeVision.position : transform.position;
        Gizmos.DrawWireSphere(centroGizmo, rangoAtaque); 
        
        if (attackPoint != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, radiusPunch);
        }
    }
}