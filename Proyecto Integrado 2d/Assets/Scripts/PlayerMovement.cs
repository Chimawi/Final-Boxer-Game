using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
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

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer[] partesDelCuerpo; 

    private float inputHorizontal;
    
    // Estados
    private bool isAttacking = false;
    private bool isBlocking = false; 
    private bool isHurt = false; 
    private bool isDead = false; 
    private bool isVictory = false;
    
    // --- NUEVO: ESTADO DE DIÁLOGO ---
    private bool isTalking = false; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        if (pantallaDerrota != null) pantallaDerrota.SetActive(false);
    }

    void Update()
    {
        // --- NUEVO: AÑADIDO 'isTalking' AL BLOQUEO DE INPUTS ---
        if (isDead || isHurt || isVictory || isTalking) return;

        if (isAttacking || isBlocking)
        {
            inputHorizontal = 0; 
            return; 
        }

        GestionarInputsCombate();
        GestionarMovimiento();
    }

    // --- NUEVA FUNCIÓN PÚBLICA PARA EL NPC ---
    public void SetEstadoDialogo(bool estado)
    {
        isTalking = estado;

        if (isTalking)
        {
            // Si empezamos a hablar, frenamos en seco y reseteamos animaciones
            rb.linearVelocity = Vector2.zero;
            inputHorizontal = 0;
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
        }
    }
    // -----------------------------------------

    void GestionarInputsCombate()
    {
        if (Input.GetKeyDown(KeyCode.V)) 
        {
            if (Time.time >= tiempoSiguienteBloqueo)
            {
                StartBlock();
                tiempoSiguienteBloqueo = Time.time + cooldownCombate;
                Debug.Log("Player: Bloqueo activado.");
            }
        }
        else if (Input.GetKeyDown(KeyCode.C)) 
        {
            if (Time.time >= tiempoSiguienteAtaque)
            {
                StartAttack();
                tiempoSiguienteAtaque = Time.time + cooldownCombate;
                Debug.Log("Player: Ataque lanzado.");
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
        // --- NUEVO: AÑADIDO 'isTalking' ---
        if (!isHurt && !isDead && !isVictory && !isTalking)
        {
            rb.linearVelocity = new Vector2(inputHorizontal * velocity, rb.linearVelocity.y);
        }
    }

    // ... (EL RESTO DEL SCRIPT SIGUE IGUAL: RecibirDaño, Morir, DetectarGolpe, etc.)
    
    // Solo asegurate de que todo lo demás esté igual que en el script anterior
    // por brevedad no copio las funciones de daño aquí, pero no las borres.

    // --- COPIA PEGA EL RESTO DE FUNCIONES (RecibirDaño, Morir, Rutinas, etc) AQUÍ ---
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

    public void RecibirAturdimientoPorBloqueo()
    {
        if (isDead || isVictory) return;
        StartCoroutine(RutinaStunAmarillo());
    }

    IEnumerator RutinaStunAmarillo()
    {
        isHurt = true; 
        isAttacking = false; 
        isBlocking = false;
        animator.SetTrigger("Hurt"); 
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(EfectoParpadeo(Color.yellow));
        yield return new WaitForSeconds(tiempoAturdimiento + 0.5f); 
        isHurt = false;
        rb.linearVelocity = Vector2.zero;
    }

    IEnumerator EfectoBloqueo(Vector2 direccion)
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 empujeSuave = new Vector2(direccion.x, 0).normalized;
        rb.AddForce(empujeSuave * (fuerzaEmpuje / 2), ForceMode2D.Impulse); 
        yield return null; 
    }

    IEnumerator RutinaEmpujeYMuerte(Vector2 direccion)
    {
        isHurt = true; 
        isAttacking = false; 
        isBlocking = false;
        animator.SetTrigger("Hurt"); 
        rb.linearVelocity = Vector2.zero;
        Vector2 empujeHorizontal = new Vector2(direccion.x, 0).normalized;
        rb.AddForce(empujeHorizontal * fuerzaEmpuje, ForceMode2D.Impulse);
        yield return new WaitForSeconds(tiempoAturdimiento);
        if (vida <= 0) Morir();
        else { isHurt = false; rb.linearVelocity = Vector2.zero; }
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

    void Morir()
    {
        if (isDead) return;
        isDead = true; 
        animator.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        GetComponent<Collider2D>().enabled = false; 
        EnemyAI[] listaEnemigos = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemigo in listaEnemigos) if (enemigo != null) enemigo.ActivarVictoria();
        StartCoroutine(RutinaDerrota());
    }

    IEnumerator RutinaDerrota()
    {
        yield return new WaitForSeconds(4.0f);
        if (pantallaDerrota != null) pantallaDerrota.SetActive(true);
        this.enabled = false; 
    }

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
        yield return new WaitForSeconds(3.0f); 
        animator.SetTrigger("Victory");
    }

    void StartAttack() { isAttacking = true; animator.SetTrigger("Attack"); }
    public void FinishAttack() { isAttacking = false; }
    void StartBlock() { isBlocking = true; animator.SetTrigger("Block"); }
    public void FinishBlock() { isBlocking = false; }
    
    public void DetectarGolpe()
    {
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);
        foreach (Collider2D colision in objetosGolpeados)
        {
            if (colision is BoxCollider2D) continue; 
            EnemyAI enemigoScript = colision.GetComponent<EnemyAI>();
            if (enemigoScript != null)
            {
                Vector2 direccionEmpuje = (enemigoScript.transform.position - transform.position).normalized;
                enemigoScript.RecibirDaño(1f, direccionEmpuje, this);
            }
            SacoBoxeo sacoScript = colision.GetComponent<SacoBoxeo>();
            if (sacoScript != null) sacoScript.Golpeado();
        }
    }
}