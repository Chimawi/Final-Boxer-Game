using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("Configuración de UI")]
    [Tooltip("Arrastra aquí el objeto visual de la tecla E")]
    public GameObject teclaEPrompt;

    [Tooltip("Arrastra aquí el panel del diálogo")]
    public GameObject panelDialogo;

    private bool jugadorCerca;

    void Start()
    {
        // Aseguramos que al iniciar el juego la UI esté oculta
        teclaEPrompt.SetActive(false);
        panelDialogo.SetActive(false);
        jugadorCerca = false;
    }

    void Update()
    {
        // Si el jugador está cerca y presiona E
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            AbrirOCerrarDialogo();
        }
    }

    void AbrirOCerrarDialogo()
    {
        // Si el diálogo ya está activo, lo cerramos. Si no, lo abrimos.
        bool estadoActual = panelDialogo.activeSelf;
        panelDialogo.SetActive(!estadoActual);

        // Opcional: Ocultar la 'E' mientras se habla para que se vea más limpio
        if (!estadoActual)
        {
            teclaEPrompt.SetActive(false);
        }
    }

    // Detectar cuando el jugador entra en el área
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = true;
            // Solo mostramos la 'E' si no estamos ya hablando
            if (!panelDialogo.activeSelf)
            {
                teclaEPrompt.SetActive(true);
            }
        }
    }

    // Detectar cuando el jugador sale del área
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = false;
            teclaEPrompt.SetActive(false);
            panelDialogo.SetActive(false); // Cierra el diálogo si te alejas
        }
    }
}









