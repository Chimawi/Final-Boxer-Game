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

    [Header("IA - Combate y Precisión")]
    public float rangoAtaque = 0.8f; 
    public float cooldownAcciones = 3.0f;
    private float tiempoSiguienteAccion = 0f;
    
    [Header("Referencia Ojos/Pecho")]
    public Transform puntoDeVision; // ARRASTRA AQUÍ EL OBJETO HIJO (PECHO)

    [Header("UI - Barra de Vida")]
    public BarraDeVida barraDeVidaScript; 

    [Header("Referencias")]
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer[] partesDelCuerpo; 
    private Transform playerTransform; 

    // Estados
    private bool estaAturdido = false; 
    private bool estaContraatacando = false; 
    public bool jugadorDetectado = false; 
    private bool celebrandoVictoria = false; 
    
    // --- NUEVO: ESTADO DE DIÁLOGO ---
    public bool enDialogo = false; 

    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f;
    
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
        
        ElegirNuevaDireccionAleatoria();
    }

    void Update()
    {
        // BLOQUEO TOTAL: Si está muerto, aturdido, celebrando O HABLANDO, no se mueve.
        if (vida <= 0 || estaAturdido || estaContraatacando || celebrandoVictoria || enDialogo) return;
        
        if (jugadorDetectado && playerTransform != null)
        {
            ComportamientoCombate();
        }
        else
        {
            Patrullar();
        }
    }

    // --- NUEVA FUNCIÓN PARA CONGELAR AL ENEMIGO DURANTE LA INTRO ---
    public void SetEstadoDialogo(bool estado)
    {
        enDialogo = estado;

        if (enDialogo)
        {
            // 1. Frenamos en seco la física
            rb.linearVelocity = Vector2.zero;
            
            // 2. Reseteamos todas las animaciones para que se quede quieto (Idle)
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Block");
            
            // 3. Detenemos cualquier ataque o patrulla en proceso
            StopAllCoroutines(); 
        }
        else
        {
            // Al salir del diálogo, reseteamos el cooldown para que no te pegue instantáneamente
            tiempoSiguienteAccion = Time.time + 1.0f; 
            ElegirNuevaDireccionAleatoria(); // Reactivar patrulla o combate
        }
    }
    // -------------------------------------------------------------

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

    void ComportamientoCombate()
    {
        Vector2 miPosicion = (puntoDeVision != null) ? puntoDeVision.position : transform.position;
        
        Vector2 posicionJugador;
        Collider2D colliderJugador = playerTransform.GetComponent<Collider2D>();
        if (colliderJugador != null) posicionJugador = colliderJugador.bounds.center;
        else posicionJugador = playerTransform.position;

        float distanciaJugador = Vector2.Distance(miPosicion, posicionJugador);
        
        if (distanciaJugador <= rangoAtaque)
        {
            rb.linearVelocity = Vector2.zero;
            DetenerAnimacionesMovimiento();
            
            float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
            direccionMovimiento = (int)dirX; 

            if (Time.time >= tiempoSiguienteAccion)
            {
                DecidirAccion();
            }
        }
        else
        {
            PerseguirJugador();
        }
    }

    void DecidirAccion()
    {
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
        float decision = Random.value;
        if (decision > 0.3f) 
        {
            animator.SetTrigger("Attack");
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

    public void RecibirDaño(float daño, Vector2 direccionEmpuje, PlayerMovement atacante = null)
    {
        if (vida <= 0) return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
            Debug.Log("Enemigo: ¡BLOQUEO! Contraatacando...");
            if (atacante != null) atacante.RecibirAturdimientoPorBloqueo();
            
            StartCoroutine(EfectoBloqueo(direccionEmpuje));
            float direccionHaciaJugador = -Mathf.Sign(direccionEmpuje.x);
            StartCoroutine(RutinaContraataque(direccionHaciaJugador));
            return;
        }

        vida -= daño;
        if (barraDeVidaScript != null) barraDeVidaScript.CambiarVidaActual(vida, vidaMaxima);

        StartCoroutine(RutinaEmpuje(direccionEmpuje));
        StartCoroutine(EfectoParpadeo(Color.red)); 
    }

    IEnumerator RutinaContraataque(float dirX)
    {
        estaContraatacando = true; 
        yield return new WaitForSeconds(0.2f);

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

        rb.linearVelocity = Vector2.zero;
        DetenerAnimacionesMovimiento(); 
        animator.ResetTrigger("Block"); 
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.8f);
        estaContraatacando = false; 
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
    }

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
        
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsBackWalking", false);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Block");

        StartCoroutine(RutinaVictoriaEnemigo());
    }

    IEnumerator RutinaVictoriaEnemigo()
    {
        yield return new WaitForSeconds(2.0f); 
        Debug.Log("Enemigo: ¡VICTORIA!");
        animator.SetTrigger("Victory");
    }

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
                player.RecibirDaño(10f, direccionEmpuje, this); 
            }
        }
    }
}