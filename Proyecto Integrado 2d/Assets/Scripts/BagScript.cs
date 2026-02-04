using UnityEngine;

public class SacoBoxeo : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Esta función la llamará el Player cuando su puño toque el saco
    public void Golpeado()
    {
        anim.SetTrigger("Hit");
        
        // Opcional: Sonido de golpe aquí más adelante
        Debug.Log("¡El saco ha sido golpeado!");
    }
}






