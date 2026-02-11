using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))] // Añadimos AudioSource
public class PlayerMovement : MonoBehaviour
{
    [Header("Estadísticas")]
    public float vida = 100f; 
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f;
    public float tiempoAturdimiento = 0.5f;

    [Header("Cooldowns")]
    public float cooldownCombate = 3.0f; 
    private float tiempoSiguienteAtaque = 0f;
    private float tiempoSiguienteBloqueo = 0f;

    [Header("UI")]
    public BarraDeVida barraDeVidaScript; 
    public GameObject pantallaDerrota; 

    [Header("Movimiento")]
    public float velocity = 5f;

    [Header("Combate")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask enemysLayer; 
    public bool combateIniciado = false; 

    // --- NUEVO: AUDIO COMBATE ---
    [Header("Audio Combate")]
    public AudioClip[] sfxLanzarGolpe; // Sonido al aire (Whoosh)
    public AudioClip[] sfxImpacto;     // Sonido al pegar (Pum!)
    private AudioSource audioSource;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer[] partesDelCuerpo; 
    private float inputHorizontal;
    
    private bool isAttacking = false;
    private bool isBlocking = false; 
    private bool isHurt = false; 
    private bool isDead = false; 
    private bool isVictory = false;
    private bool isTalking = false; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); // Obtenemos el componente de audio
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        if (pantallaDerrota != null) pantallaDerrota.SetActive(false);
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

    // ... (El resto de funciones SetEstadoDialogo, GestionarMovimiento, FixedUpdate siguen igual) ...

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
    
    void GestionarMovimiento()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        if (inputHorizontal > 0) { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); }
        else if (inputHorizontal < 0) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); }
        else { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", false); }
    }

    void FixedUpdate()
    {
        if (!isHurt && !isDead && !isVictory && !isTalking && combateIniciado)
        {
            rb.linearVelocity = new Vector2(inputHorizontal * velocity, rb.linearVelocity.y);
        }
    }

    // --- LÓGICA DE ATAQUE CON SONIDO ---
    void StartAttack() 
    { 
        isAttacking = true; 
        animator.SetTrigger("Attack"); 
        
        // SONIDO: LANZAMIENTO (WHOOSH)
        ReproducirSonidoAleatorio(sfxLanzarGolpe);
    }
    
    public void DetectarGolpe()
    {
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);
        bool golpeAcertado = false; // Para saber si dimos a algo y reproducir sonido de impacto

        foreach (Collider2D colision in objetosGolpeados)
        {
            if (colision is BoxCollider2D) continue; 

            EnemyAI enemigoScript = colision.GetComponent<EnemyAI>();
            if (enemigoScript != null)
            {
                Vector2 direccionEmpuje = (enemigoScript.transform.position - transform.position).normalized;
                enemigoScript.RecibirDaño(20f, direccionEmpuje, this);
                golpeAcertado = true;
            }

            SacoBoxeo sacoScript = colision.GetComponent<SacoBoxeo>();
            if (sacoScript != null)
            {
                sacoScript.Golpeado();
                golpeAcertado = true;
            }
        }

        // SONIDO: IMPACTO (SOLO SI DIMOS A ALGO)
        if (golpeAcertado)
        {
            ReproducirSonidoAleatorio(sfxImpacto);
        }
    }
    
    // --- FUNCIÓN AUXILIAR PARA SONIDOS RANDOM ---
    void ReproducirSonidoAleatorio(AudioClip[] clips)
    {
        if (clips.Length > 0 && audioSource != null)
        {
            // Variamos ligeramente el tono para que no suene robótico
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }

    // ... (El resto de funciones RecibirDaño, Morir, etc. siguen igual, cópialas del anterior si las borraste) ...
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
    public void ActivarVictoria()
    {
        if (isVictory) return; isVictory = true; 
        isAttacking = false; isBlocking = false; inputHorizontal = 0; rb.linearVelocity = Vector2.zero;
        animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", false);
        StartCoroutine(RutinaVictoria());
    }
    IEnumerator RutinaVictoria() { yield return new WaitForSeconds(3.0f); animator.SetTrigger("Victory"); }
}