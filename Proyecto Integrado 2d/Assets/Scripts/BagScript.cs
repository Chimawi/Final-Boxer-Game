using UnityEngine;

public class OtroScript : MonoBehaviour
{Animator animator;
    public PlayerMovement playerMovement;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        if (playerMovement.ispunching = true) {
            animator.SetTrigger("IsHitingBox");
}


    }
}







