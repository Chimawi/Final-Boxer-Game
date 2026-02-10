using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    // ==========================================
    // VARIABLES DE CONFIGURACIÓN
    // ==========================================

    [Header("Estadísticas Vitales")]
    public float vida = 100f;
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f; 
    public float tiempoAturdimiento = 0.5f; 

    [Header("IA - Configuración de Combate")]
    public float rangoAtaque = 1.6f;      // Distancia a la que se detiene para pegar
    public float cooldownAcciones = 3.0f; // Tiempo de espera entre acciones
    public float retrasoAntesDeGolpe = 0.5f; // NUEVO: Tiempo que espera antes de soltar el golpe
    private float tiempoSiguienteAccion = 0f;

    [Header("UI")]
    public BarraDeVida barraDeVidaScript; 

    [Header("Referencias Físicas")]
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer[] partesDelCuerpo; 
    private Transform playerTransform; 

    // ==========================================
    // ESTADOS INTERNOS
    // ==========================================
    private bool estaAturdido = false; 
    private bool estaContraatacando = false; 
    public bool jugadorDetectado = false; 

    [Header("Movimiento")]
    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f;
    private float tiempoTranscurrido;
    private int direccionMovimiento = 1;

    [Header("Hitbox de Ataque")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask layerJugador; 


    // ==========================================
    // MÉTODOS PRINCIPALES
    // ==========================================

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();
        vidaMaxima = vida;
        
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        
        ElegirNuevaDireccionAleatoria();
    }

    void Update()
    {
        if (vida <= 0 || estaAturdido || estaContraatacando) return;
        
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
    // LÓGICA DE IA (CEREBRO)
    // ==========================================

    void ComportamientoCombate()
    {
        float distancia = Vector2.Distance(transform.position, playerTransform.position);

        if (distancia <= rangoAtaque)
        {
            // FASE 1: COMBATE (Cerca)
            rb.linearVelocity = Vector2.zero;
            DetenerAnimacionesMovimiento();
            MirarAlJugador();

            if (Time.time >= tiempoSiguienteAccion)
            {
                DecidirAccion();
            }
        }
        else
        {
            // FASE 2: PERSECUCIÓN (Lejos)
            PerseguirJugador();
        }
    }

    void DecidirAccion()
    {
        // Reseteamos el cooldown global (3 segundos para la siguiente decisión)
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
        
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Block");

        float decision = Random.value; // 0.0 a 1.0

        if (decision > 0.3f) // 70% Probabilidad
        {
            // --- CAMBIO AQUÍ: Usamos una corrutina para el retraso ---
            StartCoroutine(RealizarAtaqueConRetraso());
        }
        else // 30% Probabilidad
        {
            Debug.Log("IA: Posición DEFENSA");
            animator.SetTrigger("Block");
        }
    }

    // --- NUEVA CORRUTINA PARA EL RETRASO DEL GOLPE ---
    IEnumerator RealizarAtaqueConRetraso()
    {
        // 1. Esperamos el tiempo configurado (0.5s)
        // El enemigo se quedará quieto mirándote (tensión)
        yield return new WaitForSeconds(retrasoAntesDeGolpe);

        // 2. Si no ha muerto ni ha sido aturdido en ese tiempo, ataca
        if (!estaAturdido && vida > 0)
        {
            Debug.Log("IA: ¡Lanzando ATAQUE tras espera!");
            animator.SetTrigger("Attack");
        }
    }

    void PerseguirJugador()
    {
        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
        direccionMovimiento = (int)dirX;
        
        rb.linearVelocity = new Vector2(direccionMovimiento * velocidad, rb.linearVelocity.y);
        ActualizarAnimacionesMovimiento();
    }

    void MirarAlJugador()
    {
        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
        direccionMovimiento = (int)dirX;
    }


    // ==========================================
    // MOVIMIENTO Y PATRULLA
    // ==========================================

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

    void ElegirNuevaDireccionAleatoria()
    {
        int aleatorio = Random.Range(0, 2);
        direccionMovimiento = (aleatorio == 0) ? -1 : 1;
        ActualizarAnimacionesMovimiento();
    }

    void ActualizarAnimacionesMovimiento() 
    { 
        if (direccionMovimiento == 1) 
        { 
            animator.SetBool("IsWalking", false); 
            animator.SetBool("IsBackWalking", true); 
        } 
        else 
        { 
            animator.SetBool("IsWalking", true); 
            animator.SetBool("IsBackWalking", false); 
        } 
    }
    
    void DetenerAnimacionesMovimiento() 
    { 
        animator.SetBool("IsWalking", false); 
        animator.SetBool("IsBackWalking", false); 
    }


    // ==========================================
    // DETECCIÓN DE ZONA (TRIGGERS)
    // ==========================================

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


    // ==========================================
    // SISTEMA DE DAÑO Y CONTRAATAQUE
    // ==========================================

    public void RecibirDaño(float daño, Vector2 direccionEmpuje, PlayerMovement atacante = null)
    {
        if (vida <= 0) return;

        // VERIFICAR BLOQUEO
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
            if (atacante != null) atacante.RecibirAturdimientoPorBloqueo();
            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            
            float dirContra = -Mathf.Sign(direccionEmpuje.x);
            StartCoroutine(RutinaContraataque(dirContra));
            return;
        }

        // DAÑO NORMAL
        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);

        StartCoroutine(RutinaEmpuje(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red)); 
    }

    IEnumerator RutinaContraataque(float dirX)
    {
        estaContraatacando = true; 
        yield return new WaitForSeconds(0.2f); // Pequeña pausa de reacción

        // Avance agresivo
        float velocidadAtaque = velocidad * 2f; 
        rb.linearVelocity = new Vector2(dirX * velocidadAtaque, rb.linearVelocity.y);

        if (dirX > 0) {
             animator.SetBool("IsWalking", false); 
             animator.SetBool("IsBackWalking", true); 
        } else {
             animator.SetBool("IsWalking", true); 
             animator.SetBool("IsBackWalking", false);
        }

        yield return new WaitForSeconds(0.4f);

        // Frenar y Golpear (Aquí NO ponemos el retraso de 0.5s porque es un contraataque rápido)
        rb.linearVelocity = Vector2.zero;
        DetenerAnimacionesMovimiento(); 
        
        animator.ResetTrigger("Block"); 
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.8f);
        
        estaContraatacando = false; 
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
    }


    // ==========================================
    // CORRUTINAS DE EFECTOS
    // ==========================================

    public void RecibirAturdimientoPorBloqueo()
    {
        if (vida <= 0) return;
        StartCoroutine(RutinaStunAmarillo());
    }

    IEnumerator RutinaStunAmarillo()
    {
        estaAturdido = true;
        animator.SetTrigger("Hurt"); 
        rb.linearVelocity = Vector2.zero; 
        StartCoroutine(EfectoParpadeo(Color.yellow));
        yield return new WaitForSeconds(tiempoAturdimiento + 1.0f); 
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

    void Morir()
    {
        animator.SetTrigger("Die");
        estaAturdido = true; 
        GetComponent<Collider2D>().enabled = false; 
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        this.enabled = false; 
    }


    // ==========================================
    // ATAQUE (DetectarGolpe) Y GIZMOS
    // ==========================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        
        if (attackPoint != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(attackPoint.position, radiusPunch);
        }
    }

    public void DetectarGolpe()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, layerJugador);
        
        foreach (Collider2D hit in hits)
        {
            if (hit is BoxCollider2D) continue; 

            PlayerMovement player = hit.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Vector2 direccionEmpuje = (player.transform.position - transform.position).normalized;
                player.RecibirDaño(1f, direccionEmpuje, this); 
            }
        }
    }
}