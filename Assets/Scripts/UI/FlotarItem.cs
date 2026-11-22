using UnityEngine;

public class FlotarItem : MonoBehaviour
{
    [Header("Ajustes de Flotación")]
    public float amplitud = 0.15f;       // Qué tanto sube y baja (no muy alto para que no se pierda)
    public float velocidadFlote = 2f;    // Qué tan rápido sube y baja

    [Header("Ajustes de Rotación")]
    public float velocidadGiro = 30f;    // Grados por segundo que gira

    private Vector3 posicionInicial;

    void Start()
    {
        // Guardamos la posición exacta donde apareció el ítem al morir el enemigo
        posicionInicial = transform.position;
    }

    void Update()
    {
        // 1. Hacer que flote suavemente usando una onda matemática (Seno)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * velocidadFlote) * amplitud;
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);

        // 2. Hacer que gire sobre su propio eje Z
        transform.Rotate(0, 0, velocidadGiro * Time.deltaTime);
    }   
}
