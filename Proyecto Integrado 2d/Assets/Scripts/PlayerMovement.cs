using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    // ==========================================
    // 1. CONFIGURACIÓN
    // ==========================================
    [Header("Estadísticas")]
    public float vida = 100f; 
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f;
    public float tiempoAturdimiento = 0.5f;

    [Header("UI - Pantallas")]
    public BarraDeVida barraDeVidaScript; 
    public GameObject pantallaDerrota;  
    public GameObject pantallaVictoria; 

    [Header("Movimiento")]
    public float velocity = 5f;

    [Header("Combate")]
    public float cooldownCombate = 3.0f; 
    private float tiempoSiguienteAtaque = 0f;
    private float tiempoSiguienteBloqueo = 0f;
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask enemysLayer; 
    
    [Header("Audio Pasos")]
    public AudioClip[] sfxPasos;
    public float ritmoPasos = 0.4f;
    private float siguientePaso = 0f;

    [Header("Audio Combate")]
    public AudioClip[] sfxLanzarGolpe; 
    public AudioClip[] sfxImpacto;     
    private AudioSource audioSource;

    // ==========================================
    // 2. ESTADOS
    // ==========================================
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer[] partesDelCuerpo; 
    private float inputHorizontal;
    
    public bool combateIniciado = false; 
    private bool isAttacking = false;
    private bool isBlocking = false; 
    private bool isHurt = false; 
    private bool isDead = false; 
    private bool isVictory = false;
    private bool isTalking = false; 

    // ==========================================
    // 3. CICLO DE VIDA
    // ==========================================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); 
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        
        if (pantallaDerrota != null) pantallaDerrota.SetActive(false);
        if (pantallaVictoria != null) pantallaVictoria.SetActive(false); 
        
        combateIniciado = false; 
    }

    void Update()
    {
        if (!combateIniciado || isDead || isHurt || isVictory || isTalking) return;

        if (isAttacking || isBlocking)
        {
            inputHorizontal = 0; 
            return; 
        }

        GestionarInputsCombate();
        GestionarMovimiento();
    }

    void FixedUpdate()
    {
        if (!isHurt && !isDead && !isVictory && !isTalking && combateIniciado)
        {
            rb.linearVelocity = new Vector2(inputHorizontal * velocity, rb.linearVelocity.y);
        }
    }

    // ==========================================
    // 4. LÓGICA
    // ==========================================
    void GestionarMovimiento()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        if (inputHorizontal > 0) { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); }
        else if (inputHorizontal < 0) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); }
        else { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", false); }

        if (inputHorizontal != 0)
        {
            if (Time.time >= siguientePaso)
            {
                ReproducirSonidoAleatorio(sfxPasos, 0.8f, 1.2f, 0.3f);
                siguientePaso = Time.time + ritmoPasos;
            }
        }
    }

    public void SetEstadoDialogo(bool estado)
    {
        isTalking = estado;
        if (isTalking)
        {
            rb.linearVelocity = Vector2.zero;
            inputHorizontal = 0;
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
        }
    }

    void GestionarInputsCombate()
    {
        if (Input.GetKeyDown(KeyCode.V)) 
        {
            if (Time.time >= tiempoSiguienteBloqueo)
            {
                StartBlock();
                tiempoSiguienteBloqueo = Time.time + cooldownCombate;
            }
        }
        else if (Input.GetKeyDown(KeyCode.C)) 
        {
            if (Time.time >= tiempoSiguienteAtaque)
            {
                StartAttack();
                tiempoSiguienteAtaque = Time.time + cooldownCombate;
            }
        }
    }

    void StartAttack() 
    { 
        isAttacking = true; 
        animator.SetTrigger("Attack"); 
        ReproducirSonidoAleatorio(sfxLanzarGolpe);
    }
    
    // --- AQUÍ ESTABA EL ERROR, YA CORREGIDO ---
    public void DetectarGolpe()
    {
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);
        bool golpeAcertado = false; 

        foreach (Collider2D colision in objetosGolpeados)
        {
            if (colision is BoxCollider2D) continue; 

            // 1. Detectar Enemigo
            EnemyAI enemigoScript = colision.GetComponent<EnemyAI>();
            if (enemigoScript != null)
            {
                Vector2 direccionEmpuje = (enemigoScript.transform.position - transform.position).normalized;
                enemigoScript.RecibirDaño(20f, direccionEmpuje, this);
                golpeAcertado = true;
            }

            // 2. Detectar Saco de Boxeo (¡Esto faltaba!)
            SacoBoxeo sacoScript = colision.GetComponent<SacoBoxeo>();
            if (sacoScript != null)
            {
                sacoScript.Golpeado();
                golpeAcertado = true;
            }
        }
        
        if (golpeAcertado) ReproducirSonidoAleatorio(sfxImpacto);
    }
    // ------------------------------------------
    
    void ReproducirSonidoAleatorio(AudioClip[] clips, float pitchMin = 0.9f, float pitchMax = 1.1f, float volumen = 1.0f)
    {
        if (clips.Length > 0 && audioSource != null)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], volumen);
        }
    }

    public void FinishAttack() { isAttacking = false; }
    void StartBlock() { isBlocking = true; animator.SetTrigger("Block"); }
    public void FinishBlock() { isBlocking = false; }

    public void RecibirDaño(float daño, Vector2 direccionEmpuje, EnemyAI atacante = null)
    {
        if (isDead || isVictory) return; 
        if (isBlocking)
        {
            if (atacante != null) atacante.RecibirAturdimientoPorBloqueo();
            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            return; 
        }
        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);
        StartCoroutine(RutinaEmpujeYMuerte(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red)); 
    }
    
    public void RecibirAturdimientoPorBloqueo() { if (!isDead && !isVictory) StartCoroutine(RutinaStunAmarillo()); }

    IEnumerator RutinaStunAmarillo()
    {
        isHurt = true; isAttacking = false; isBlocking = false;
        animator.SetTrigger("Hurt"); rb.linearVelocity = Vector2.zero;
        StartCoroutine(EfectoParpadeo(Color.yellow));
        yield return new WaitForSeconds(tiempoAturdimiento + 0.5f); 
        isHurt = false; rb.linearVelocity = Vector2.zero;
    }
    
    IEnumerator EfectoBloqueo(Vector2 direccion)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(direccion.x, 0).normalized * (fuerzaEmpuje / 2), ForceMode2D.Impulse); 
        yield return null; 
    }
    
    IEnumerator RutinaEmpujeYMuerte(Vector2 direccion)
    {
        isHurt = true; isAttacking = false; isBlocking = false;
        animator.SetTrigger("Hurt"); rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(direccion.x, 0).normalized * fuerzaEmpuje, ForceMode2D.Impulse);
        yield return new WaitForSeconds(tiempoAturdimiento);
        if (vida <= 0) Morir(); else { isHurt = false; rb.linearVelocity = Vector2.zero; }
    }
    
    IEnumerator EfectoParpadeo(Color colorObjetivo)
    {
        Color colorNormal = Color.white; 
        for (int i = 0; i < 3; i++) {
            foreach (SpriteRenderer p in partesDelCuerpo) if(p) p.color = colorObjetivo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer p in partesDelCuerpo) if(p) p.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (SpriteRenderer p in partesDelCuerpo) if(p) p.color = colorNormal;
    }
    
    void Morir()
    {
        if (isDead) return; isDead = true; 
        animator.SetTrigger("Die"); rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; 
        GetComponent<Collider2D>().enabled = false; 
        EnemyAI[] listaEnemigos = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI e in listaEnemigos) if (e != null) e.ActivarVictoria();
        StartCoroutine(RutinaDerrota());
    }
    
    IEnumerator RutinaDerrota() { yield return new WaitForSeconds(4.0f); if (pantallaDerrota != null) pantallaDerrota.SetActive(true); this.enabled = false; }
    
    // ==========================================
    // 5. SISTEMA DE VICTORIA
    // ==========================================
    public void ActivarVictoria()
    {
        if (isVictory) return; 
        isVictory = true; 
        
        isAttacking = false; 
        isBlocking = false; 
        inputHorizontal = 0; 
        rb.linearVelocity = Vector2.zero;
        
        animator.SetBool("IsWalking", false); 
        animator.SetBool("IsBackWalking", false);
        
        StartCoroutine(RutinaVictoria());
    }
    
    IEnumerator RutinaVictoria() 
    { 
        yield return new WaitForSeconds(2.0f); 
        animator.SetTrigger("Victory"); 
        yield return new WaitForSeconds(2.0f);
        if (pantallaVictoria != null) pantallaVictoria.SetActive(true);
    }
}