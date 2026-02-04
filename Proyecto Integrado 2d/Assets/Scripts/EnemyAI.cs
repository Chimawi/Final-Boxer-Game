using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Configuración Movimiento")]
    public float velocidad = 3f;
    
    [Header("Configuración IA")]
    public Transform playerTarget;
    public Transform centroCuerpo;
    public float distanciaAtaque = 2.0f;    // Aumentado para probar
    public float distanciaRetirada = 1.0f; 

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer; // Para cambiar color
    private float moveDirection; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Forzamos la búsqueda
        if (playerTarget == null)
        {
            var jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) playerTarget = jugador.transform;
        }
    }

    void Update()
    {
        // 1. CHEQUEO DE SEGURIDAD
        if (playerTarget == null) 
        {
            Debug.LogError("❌ EL ENEMIGO NO ENCUENTRA AL JUGADOR (Revisar TAG 'Player')");
            return;
        }

        // 2. CALCULAR DISTANCIA
        float miX = (centroCuerpo != null) ? centroCuerpo.position.x : transform.position.x;
        float distanciaX = Mathf.Abs(miX - playerTarget.position.x);

        // 3. LÓGICA CON COLORES (DEBUG)
        if (distanciaX > distanciaAtaque)
        {
            // LEJOS -> ACERCARSE (VERDE)
            moveDirection = (miX > playerTarget.position.x) ? -1f : 1f;
            spriteRenderer.color = Color.green; 
        }
        else if (distanciaX < distanciaRetirada)
        {
            // CERCA -> RETIRARSE (ROJO)
            moveDirection = (miX > playerTarget.position.x) ? 1f : -1f;
            spriteRenderer.color = Color.red;
        }
        else
        {
            // ZONA NEUTRA -> QUIETO (AMARILLO)
            moveDirection = 0f;
            spriteRenderer.color = Color.yellow;
        }

        // 4. ANIMACIONES (Simplificado)
        if (moveDirection != 0)
        {
             // Si se mueve, activamos caminar
             animator.SetBool("IsWalking", true);
             // Para simplificar test, desactivamos backwalk por ahora
             animator.SetBool("IsBackWalking", false);
        }
        else
        {
             animator.SetBool("IsWalking", false);
        }
    }

    void FixedUpdate()
    {
        // MOVIMIENTO FÍSICO
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDirection * velocidad, rb.linearVelocity.y);
        }
    }
    
    void OnDrawGizmos()
    {
        if (centroCuerpo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(centroCuerpo.position, distanciaAtaque);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(centroCuerpo.position, distanciaRetirada);
        }
    }
}
