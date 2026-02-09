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

    [Header("UI - Barra de Vida")]
    public BarraDeVida barraDeVidaScript;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
    }

    void Update()
    {
        if (isDead || isHurt) return;

        if (isAttacking || isBlocking)
        {
            inputHorizontal = 0;
            return;
        }

        GestionarInputsCombate();
        GestionarMovimiento();
    }

    void GestionarInputsCombate()
    {
        if (Input.GetKeyDown(KeyCode.V)) StartBlock();
        else if (Input.GetKeyDown(KeyCode.C)) StartAttack();
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
        if (!isHurt && !isDead)
        {
            rb.linearVelocity = new Vector2(inputHorizontal * velocity, rb.linearVelocity.y);
        }
    }

    // --- SISTEMA DE DAÑO Y BLOQUEO ---

    public void RecibirDaño(float daño, Vector2 direccionEmpuje, EnemyAI atacante = null)
    {
        if (isDead) return;

        // 1. SI ESTAMOS BLOQUEANDO
        if (isBlocking)
        {
            Debug.Log("Player: ¡BLOQUEO PERFECTO!");

            if (atacante != null) atacante.RecibirAturdimientoPorBloqueo();

            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            return;
        }

        // 2. DAÑO NORMAL
        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);

        StartCoroutine(RutinaEmpujeYMuerte(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red));
    }

    public void RecibirAturdimientoPorBloqueo()
    {
        if (isDead) return;
        Debug.Log("Player: ¡Me bloquearon! Estoy aturdido.");
        StartCoroutine(RutinaStunAmarillo());
    }

    IEnumerator RutinaStunAmarillo()
    {
        isHurt = true;

        // --- FIX IMPORTANTE ---
        // Reseteamos las acciones de combate porque la animación de "Hurt" 
        // va a cancelar las de ataque/bloqueo, y sus eventos 'Finish' nunca saltarán.
        isAttacking = false;
        isBlocking = false;
        // ----------------------

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

        // --- FIX IMPORTANTE ---
        // Aquí también reseteamos por seguridad, por si te golpean mientras atacas.
        isAttacking = false;
        isBlocking = false;
        // ----------------------

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
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorObjetivo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorNormal;
    }

    void Morir()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
    }

    // --- ACCIONES ---
    void StartAttack() { isAttacking = true; animator.SetTrigger("Attack"); }
    public void FinishAttack() { isAttacking = false; }
    void StartBlock() { isBlocking = true; animator.SetTrigger("Block"); }
    public void FinishBlock() { isBlocking = false; }

    public void DetectarGolpe()
    {
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);

        foreach (Collider2D colision in objetosGolpeados)
        {
            EnemyAI enemigoScript = colision.GetComponent<EnemyAI>();

            if (enemigoScript != null)
            {
                Vector2 direccionEmpuje = (enemigoScript.transform.position - transform.position).normalized;
                enemigoScript.RecibirDaño(1f, direccionEmpuje, this);
            }
        }
    }
}