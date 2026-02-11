using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    // ==========================================
    // 1. CONFIGURACIÓN
    // ==========================================
    [Header("Estadísticas Vitales")]
    public float vida = 100f;
    private float vidaMaxima;
    public float fuerzaEmpuje = 5f; 
    public float tiempoAturdimiento = 0.5f; 

    [Header("IA Combate")]
    public float rangoAtaque = 0.8f; 
    public float cooldownAcciones = 3.0f;
    private float tiempoSiguienteAccion = 0f;
    public Transform puntoDeVision; 

    [Header("UI")]
    public BarraDeVida barraDeVidaScript; 

    // --- NUEVO: AUDIO PASOS ---
    [Header("Audio Pasos")]
    public AudioClip[] sfxPasos;
    [Tooltip("Cada cuántos segundos suena un paso")]
    public float ritmoPasos = 0.4f;
    private float siguientePaso = 0f;

    [Header("Audio Combate")]
    public float delaySonidoGolpe = 0.1f; 
    public AudioClip[] sfxLanzarGolpe; 
    public AudioClip[] sfxImpacto;     
    private AudioSource audioSource;

    [Header("Ataque")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask layerJugador; 
    
    [Header("Movimiento")]
    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f; 

    // ==========================================
    // 2. ESTADOS
    // ==========================================
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer[] partesDelCuerpo; 
    private Transform playerTransform; 

    public bool combateIniciado = false; 
    public bool enDialogo = false;       
    
    private bool estaAturdido = false; 
    private bool estaContraatacando = false; 
    public bool jugadorDetectado = false; 
    private bool celebrandoVictoria = false; 

    private float tiempoTranscurrido;
    private int direccionMovimiento = 1;

    // ==========================================
    // 3. CICLO DE VIDA
    // ==========================================
    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>(); 
        partesDelCuerpo = GetComponentsInChildren<SpriteRenderer>();

        vidaMaxima = vida;
        if (barraDeVidaScript != null) barraDeVidaScript.InicializarBarra(vida);
        
        combateIniciado = false; 
        ElegirNuevaDireccionAleatoria();
    }

    void Update()
    {
        if (!combateIniciado || vida <= 0 || estaAturdido || estaContraatacando || celebrandoVictoria || enDialogo) return;
        
        // --- LÓGICA DE SONIDO PASOS ---
        // Si la velocidad física es mayor a 0.1, nos estamos moviendo
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            if (Time.time >= siguientePaso)
            {
                ReproducirSonido(sfxPasos, 0.3f); // Volumen bajo para pasos
                siguientePaso = Time.time + ritmoPasos;
            }
        }
        // -----------------------------
        
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
    // 4. CONTROL Y MOVIMIENTO
    // ==========================================
    public void IniciarCombate()
    {
        combateIniciado = true;
        tiempoSiguienteAccion = Time.time + 1.0f;
    }

    public void SetEstadoDialogo(bool estado)
    {
        enDialogo = estado;
        if (enDialogo)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Block");
            StopAllCoroutines(); 
        }
        else ElegirNuevaDireccionAleatoria();
    }

    void ComportamientoCombate()
    {
        Vector2 miPosicion = (puntoDeVision != null) ? puntoDeVision.position : transform.position;
        Vector2 posicionJugador = playerTransform.position;
        Collider2D colliderJugador = playerTransform.GetComponent<Collider2D>();
        if (colliderJugador != null) posicionJugador = colliderJugador.bounds.center;

        if (Vector2.Distance(miPosicion, posicionJugador) <= rangoAtaque)
        {
            rb.linearVelocity = Vector2.zero;
            DetenerAnimacionesMovimiento();
            direccionMovimiento = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);

            if (Time.time >= tiempoSiguienteAccion) DecidirAccion();
        }
        else PerseguirJugador();
    }

    void DecidirAccion()
    {
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
        if (Random.value > 0.3f) 
        {
            animator.SetTrigger("Attack");
            StartCoroutine(RutinaSonidoLanzamiento());
        }
        else animator.SetTrigger("Block");
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
    // 5. COMBATE Y AUDIO
    // ==========================================
    public void DetectarGolpe()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, layerJugador);
        bool golpeAcertado = false;

        foreach (Collider2D hit in hits)
        {
            if (hit is BoxCollider2D) continue; 
            PlayerMovement player = hit.GetComponent<PlayerMovement>();
            if (player != null)
            {
                Vector2 direccionEmpuje = (player.transform.position - transform.position).normalized;
                player.RecibirDaño(10f, direccionEmpuje, this); 
                golpeAcertado = true;
            }
        }
        if (golpeAcertado) ReproducirSonido(sfxImpacto);
    }

    void ReproducirSonido(AudioClip[] clips, float volumen = 1.0f)
    {
        if (clips != null && clips.Length > 0 && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f); 
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], volumen); 
        }
    }

    IEnumerator RutinaSonidoLanzamiento()
    {
        yield return new WaitForSeconds(delaySonidoGolpe);
        ReproducirSonido(sfxLanzarGolpe);
    }

    public void RecibirDaño(float daño, Vector2 direccionEmpuje, PlayerMovement atacante = null)
    {
        if (vida <= 0) return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Block"))
        {
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

    // ==========================================
    // 6. RUTINAS
    // ==========================================
    public void RecibirAturdimientoPorBloqueo() { if (vida > 0) StartCoroutine(RutinaStunAmarillo()); }

    IEnumerator RutinaContraataque(float dirX)
    {
        estaContraatacando = true; yield return new WaitForSeconds(0.2f); 
        float velocidadAtaque = velocidad * 2f; 
        rb.linearVelocity = new Vector2(dirX * velocidadAtaque, rb.linearVelocity.y);
        
        if (dirX > 0) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); } 
        else { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); }

        yield return new WaitForSeconds(0.4f);
        rb.linearVelocity = Vector2.zero;
        DetenerAnimacionesMovimiento(); 
        
        animator.ResetTrigger("Block"); animator.SetTrigger("Attack");
        StartCoroutine(RutinaSonidoLanzamiento());

        yield return new WaitForSeconds(0.8f); 
        estaContraatacando = false; 
        tiempoSiguienteAccion = Time.time + cooldownAcciones;
    }

    IEnumerator RutinaStunAmarillo()
    {
        estaAturdido = true; animator.SetTrigger("Hurt"); rb.linearVelocity = Vector2.zero; 
        StartCoroutine(EfectoParpadeo(Color.yellow));
        yield return new WaitForSeconds(tiempoAturdimiento + 0.5f); 
        estaAturdido = false; rb.linearVelocity = Vector2.zero; 
    }

    IEnumerator EfectoBloqueo(Vector2 direccion)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(direccion.x, 0).normalized * (fuerzaEmpuje / 2), ForceMode2D.Impulse);
        yield return null;
    }

    IEnumerator RutinaEmpuje(Vector2 direccion)
    {
        estaAturdido = true; animator.SetTrigger("Hurt"); rb.linearVelocity = Vector2.zero; 
        rb.AddForce(new Vector2(direccion.x, 0).normalized * fuerzaEmpuje, ForceMode2D.Impulse);
        yield return new WaitForSeconds(tiempoAturdimiento);
        if (vida <= 0) Morir(); else { estaAturdido = false; rb.linearVelocity = Vector2.zero; }
    }
    
    IEnumerator EfectoParpadeo(Color colorObjetivo)
    {
        Color colorNormal = Color.white; 
        for (int i = 0; i < 3; i++) {
            foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorObjetivo;
            yield return new WaitForSeconds(0.1f);
            foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorNormal;
            yield return new WaitForSeconds(0.1f);
        }
        foreach (SpriteRenderer parte in partesDelCuerpo) if(parte) parte.color = colorNormal;
    }

    IEnumerator RutinaVictoriaEnemigo() { yield return new WaitForSeconds(2.0f); animator.SetTrigger("Victory"); }

    // ==========================================
    // 7. UTILIDADES
    // ==========================================
    void Morir()
    {
        animator.SetTrigger("Die"); estaAturdido = true; GetComponent<Collider2D>().enabled = false; 
        rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; 
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>(); 
        if (player != null) player.ActivarVictoria();
        this.enabled = false; 
    }

    public void ActivarVictoria()
    {
        if (celebrandoVictoria) return; celebrandoVictoria = true; StopAllCoroutines(); 
        rb.linearVelocity = Vector2.zero; DetenerAnimacionesMovimiento();
        animator.ResetTrigger("Attack"); animator.ResetTrigger("Block");
        StartCoroutine(RutinaVictoriaEnemigo());
    }

    void ElegirNuevaDireccionAleatoria() { direccionMovimiento = (Random.Range(0, 2) == 0) ? -1 : 1; ActualizarAnimacionesMovimiento(); }
    void ActualizarAnimacionesMovimiento() { if (direccionMovimiento == 1) { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", true); } else { animator.SetBool("IsWalking", true); animator.SetBool("IsBackWalking", false); } }
    void DetenerAnimacionesMovimiento() { animator.SetBool("IsWalking", false); animator.SetBool("IsBackWalking", false); }

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) { jugadorDetectado = true; playerTransform = other.transform; } }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) { jugadorDetectado = false; playerTransform = null; } }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroGizmo = (puntoDeVision != null) ? puntoDeVision.position : transform.position;
        Gizmos.DrawWireSphere(centroGizmo, rangoAtaque); 
        if (attackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(attackPoint.position, radiusPunch); }
    }
}