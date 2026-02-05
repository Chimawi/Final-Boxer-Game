using UnityEngine;
using System.Collections; // Necesario para usar IEnumerator

public class EnemyAI : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 3.0f;
    public float intervaloCambio = 2.0f;

    [Header("Configuración de Combate")]
    public float tiempoDeReaccion = 1.0f; // Tiempo que tarda en atacar tras detectar (1 segundo)
    public float duracionAnimacionCombate = 1.5f; // Tiempo para que termine la animacion de ataque

    [Header("Referencias")]
    private Animator animator;
    private float tiempoTranscurrido;
    private int direccionMovimiento = 1;
    private bool estaEnCombate = false;

    [Header("Punch")]
    public Transform attackPoint;
    public float radiusPunch = 0.5f;
    public LayerMask enemysLayer;

    void Start()
    {
        animator = GetComponent<Animator>();
        ElegirNuevaDireccion();
    }

    void Update()
    {
        // Si está en combate (esperando o atacando), no se mueve
        if (estaEnCombate) return;

        MoverEnemigo();
        GestionarTiempo();
    }

    void MoverEnemigo()
    {
        transform.Translate(Vector3.right * direccionMovimiento * velocidad * Time.deltaTime);
    }

    void GestionarTiempo()
    {
        tiempoTranscurrido += Time.deltaTime;

        if (tiempoTranscurrido >= intervaloCambio)
        {
            ElegirNuevaDireccion();
            tiempoTranscurrido = 0;
        }
    }

    void ElegirNuevaDireccion()
    {
        int aleatorio = Random.Range(0, 2);
        direccionMovimiento = (aleatorio == 0) ? -1 : 1;
        ActualizarAnimacionesMovimiento();
    }

    void ActualizarAnimacionesMovimiento()
    {
        if (direccionMovimiento == 1)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsBackWalking", false);
        }
        else
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsBackWalking", true);
        }
    }

    void DetenerAnimacionesMovimiento()
    {
        // Congelamos visualmente al enemigo
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsBackWalking", false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo iniciamos la corrutina si es el Player y NO estamos ya en combate
        if (other.CompareTag("Player") && !estaEnCombate)
        {
            StartCoroutine(SecuenciaDeCombate());
        }
    }

    // --- AQUÍ ESTÁ LA NUEVA CURRUTINA ---
    IEnumerator SecuenciaDeCombate()
    {
        estaEnCombate = true; // 1. Detenemos la lógica del Update inmediatamente
        DetenerAnimacionesMovimiento(); // 2. El enemigo se queda quieto (Idle)

        // 3. Esperamos el tiempo de reacción (1 segundo)
        // Durante este tiempo el enemigo está en Idle mirando al jugador
        yield return new WaitForSeconds(tiempoDeReaccion);

        // 4. Decidimos qué hacer (Atacar o Bloquear)
        float decision = Random.Range(0f, 1f);
        if (decision > 0.5f)
        {
            Debug.Log("IA: ¡Atacando!");
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.Log("IA: ¡Bloqueando!");
            animator.SetTrigger("Block");
        }

        // 5. Esperamos a que termine la animación del ataque/bloqueo
        // Si tus animaciones duran más o menos, ajusta 'duracionAnimacionCombate'
        yield return new WaitForSeconds(duracionAnimacionCombate);

        // 6. Volvemos a patrullar
        ReiniciarPatrulla();
    }

    void ReiniciarPatrulla()
    {
        estaEnCombate = false;
        ElegirNuevaDireccion();
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
}