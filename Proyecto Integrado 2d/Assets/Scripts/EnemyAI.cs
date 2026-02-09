using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("Estadísticas Vitales")]
    public float vida = 100f;
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f;
    public float tiempoAturdimiento = 0.5f;

    [Header("UI - Barra de Vida")]
    public BarraDeVida barraDeVidaScript;

    [Header("Referencias")]
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer[] partesDelCuerpo;

    // Estados
    private bool estaEnCombate = false;
    private bool estaAturdido = false;

    // Configs de Movimiento y Combate
    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f;
    public float tiempoDeReaccion = 1.0f;
    public float duracionAnimacionCombate = 1.5f;
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask layerJugador;

    private float tiempoTranscurrido;
    private int direccionMovimiento = 1;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();
        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        ElegirNuevaDireccion();
    }

    void Update()
    {
        if (vida <= 0 || estaAturdido || estaEnCombate) return;
        MoverEnemigo();
        GestionarTiempo();
    }

    void MoverEnemigo() { rb.linearVelocity = new Vector2(direccionMovimiento * velocidad, rb.linearVelocity.y); }
    void GestionarTiempo() { tiempoTranscurrido += Time.deltaTime; if (tiempoTranscurrido >= intervaloCambio) { ElegirNuevaDireccion(); tiempoTranscurrido = 0; } }
    void ElegirNuevaDireccion() { int aleatorio = Random.Range(0, 2); direccionMovimiento = (aleatorio == 0) ? -1 : 1; ActualizarAnimacionesMovimiento(); }
    void ActualizarAnimacionesMovimiento() { if (direccionMovimiento == 1) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); } else { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); } }
    void DetenerAnimacionesMovimiento() { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", false); rb.linearVelocity = Vector2.zero; }


    // --- SISTEMA DE DAÑO Y BLOQUEO IA MODIFICADO ---

    // Aceptamos 'PlayerMovement atacante' para devolverle el stun si bloqueamos
    public void RecibirDaño(float daño, Vector2 direccionEmpuje, PlayerMovement atacante = null)
    {
        if (vida <= 0) return;

        // 1. SI LA IA ESTÁ BLOQUEANDO
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
            Debug.Log("Enemigo: ¡BLOQUEADO! Aturdiendo al jugador...");

            // A) Aturdimiento al Jugador (El jugador se pone amarillo)
            if (atacante != null)
            {
                atacante.RecibirAturdimientoPorBloqueo();
            }

            // B) Efecto visual para el enemigo (Cyan/Azul para indicar defensa)
            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            return;
        }

        // 2. DAÑO NORMAL
        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);

        StartCoroutine(RutinaEmpuje(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red));
    }

    public void RecibirAturdimientoPorBloqueo()
    {
        if (vida <= 0) return;
        Debug.Log("Enemigo: ¡Me bloquearon! Me quedo quieto.");
        StartCoroutine(RutinaStunAmarillo());
    }

    IEnumerator RutinaStunAmarillo()
    {
        estaAturdido = true;
        animator.SetTrigger("Hurt");

        rb.linearVelocity = Vector2.zero; // Frenado en seco

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

        // El enemigo SÍ se pone Cyan al bloquear para distinguirlo del Stun Amarillo
        Color colorBloqueo = Color.cyan;
        Color colorNormal = Color.white;
        for (int i = 0; i < 2; i++)
        {
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorBloqueo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
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
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorObjetivo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (SpriteRenderer parte in partesDelCuerpo) if (parte) parte.color = colorNormal;
    }

    void Morir()
    {
        animator.SetTrigger("Die");
        estaAturdido = true;
        GetComponent<Collider2D>().enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        this.enabled = false;
    }

    // --- LOGICA DE COMBATE ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !estaEnCombate && !estaAturdido && vida > 0)
        {
            StartCoroutine(SecuenciaDeCombate());
        }
    }

    IEnumerator SecuenciaDeCombate()
    {
        estaEnCombate = true;
        DetenerAnimacionesMovimiento();
        yield return new WaitForSeconds(tiempoDeReaccion);

        if (vida > 0 && !estaAturdido)
        {
            float decision = Random.Range(0f, 1f);
            if (decision > 0.5f) animator.SetTrigger("Attack");
            else animator.SetTrigger("Block");
        }
        yield return new WaitForSeconds(duracionAnimacionCombate);
        if (vida > 0) ReiniciarPatrulla();
    }

    void ReiniciarPatrulla()
    {
        estaEnCombate = false;
        ElegirNuevaDireccion();
    }

    public void DetectarGolpe()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, layerJugador);
        foreach (Collider2D hit in hits)
        {
            PlayerMovement player = hit.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Vector2 direccionEmpuje = (player.transform.position - transform.position).normalized;
                player.RecibirDaño(1f, direccionEmpuje, this);
            }
        }
    }
}