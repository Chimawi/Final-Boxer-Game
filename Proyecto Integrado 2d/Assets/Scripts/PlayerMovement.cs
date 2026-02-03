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
    public bool ispunching = false;

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
        
        if (inputHorizontal != 0) animator.SetBool("IsWalking", true);
        else animator.SetBool("IsWalking", false);
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
        Collider2D[] objectsPunch = Physics2D.OverlapCircleAll(attackPoint.position, radiusPunch, enemysLayer);
        foreach (Collider2D enemigo in objectsPunch)
        {
            ispunching = true;
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