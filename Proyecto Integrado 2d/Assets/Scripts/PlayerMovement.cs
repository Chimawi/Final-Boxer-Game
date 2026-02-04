using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float velocity = 5f;

    [Header("Punch")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask enemysLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private float inputHorizontal;
    
    
    private bool isAttacking = false;
    private bool isBlocking = false;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        
        if (isAttacking || isBlocking)
        {
            inputHorizontal = 0; 
            return; 
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            StartBlock();
            return; 
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            StartAttack();
            return;
        }

        inputHorizontal = Input.GetAxisRaw("Horizontal");
        
        if (inputHorizontal > 0) 
        {
            // Moviendo a la DERECHA (Adelante)
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsBackWalking", false);
        }
        else if (inputHorizontal < 0)
        {
            // Moviendo a la IZQUIERDA (Atrás)
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", true);
        }
        else
        {
            // QUIETO
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", false);
        }
    }
    void FixedUpdate()
    {
        
            rb.linearVelocity = new Vector2(inputHorizontal * velocity, rb.linearVelocity.y);
        
    }

    void StartAttack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
    }

    public void FinishAttack()
    {
        isAttacking = false;
    }

    

    void StartBlock()
    {
        isBlocking = true;
        animator.SetTrigger("Block"); 
    }
    public void FinishBlock()
    {
        isBlocking = false;
    }
    
    public void DetectarGolpe()
    {
        // Detectamos todo lo que esté en el círculo de ataque y sea capa "Enemigos"
        Collider2D[] objetosGolpeados = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);

        foreach (Collider2D enemigo in objetosGolpeados)
        {
            // 1. Buscamos si el objeto golpeado tiene el script "SacoBoxeo"
            SacoBoxeo saco = enemigo.GetComponent<SacoBoxeo>();

            // 2. Si lo tiene, activamos su función Golpeado
            if (saco != null)
            {
                saco.Golpeado();
            }
            
            // Aquí añadiremos lógica para enemigos reales (con vida) más adelante
            Debug.Log("¡Golpeaste a " + enemigo.name + "!");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, radiusPunch);
    }
}