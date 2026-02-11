using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // Necesario para TextMeshPro

public class ControladorCarga : MonoBehaviour
{
    [Header("Configuración Tiempos")]
    public float tiempoDeEspera = 5.0f;
    
    [Header("Referencias UI")]
    public TextMeshProUGUI textoCarga;   // El texto que dice "Cargando..."
    public TextMeshProUGUI textoConsejo; // --- NUEVO: El texto donde saldrá la frase ---

    [Header("Biblioteca de Consejos")]
    [Tooltip("Escribe aquí todas las frases que quieras. El juego elegirá una al azar.")]
    [TextArea(2, 5)] // Esto hace que las cajas de texto en el inspector sean más grandes y cómodas
    public string[] listaDeConsejos;

    void Start()
    {
        // 1. Mostrar frase aleatoria nada más empezar
        MostrarConsejoAleatorio();

        // 2. Iniciar la cuenta atrás
        StartCoroutine(RutinaDeEspera());
    }

    void MostrarConsejoAleatorio()
    {
        // Solo intentamos mostrar algo si hay frases y si asignaste el texto
        if (listaDeConsejos.Length > 0 && textoConsejo != null)
        {
            int indiceAleatorio = Random.Range(0, listaDeConsejos.Length);
            textoConsejo.text = listaDeConsejos[indiceAleatorio];
        }
        else
        {
            Debug.LogWarning("ControladorCarga: No has asignado frases o falta el objeto de texto en el Inspector.");
        }
    }

    IEnumerator RutinaDeEspera()
    {
        if (textoCarga != null) textoCarga.text = "Cargando...";

        Debug.Log("Cargando escena... Esperando " + tiempoDeEspera + " segundos.");
        yield return new WaitForSeconds(tiempoDeEspera);

        // Recuperamos el nombre del nivel destino de la memoria
        string nivelACharger = PlayerPrefs.GetString("NivelDestino");

        if (!string.IsNullOrEmpty(nivelACharger))
        {
            SceneManager.LoadScene(nivelACharger);
        }
        else
        {
            Debug.LogError("Error: No se encontró el nombre del nivel destino. ¿Abriste la escena de carga directamente?");
            // Opcional: Volver al menú si hay error
            // SceneManager.LoadScene("MenuPrincipal"); 
        }
    }
}