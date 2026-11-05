using System.Collections;
using UnityEngine;

public class LaserAnomalia : MonoBehaviour
{
    [Header("Configuración del Láser")]
    public float distanciaMax = 30f;
    public float tiempoAdvertencia = 1.5f; // Tiempo que tarda en avisar
    public float duracionDisparo = 0.5f;     // Tiempo que dura el rayo cargado
    public float danoPorSegundo = 25f;

    [Header("Grosor del Rayo")]
    public float grosorAdvertencia = 0.15f;
    public float grosorDisparo = 1.2f;

    [Header("Colores")]
    public Color colorAdvertencia = new Color(1f, 0.9f, 0.1f, 0.4f); // Amarillo translúcido
    public Color colorDisparo = new Color(1f, 0.1f, 0.1f, 1f);      // Rojo intenso sólido

    private LineRenderer lineRenderer;
    private Transform jugador;
    private bool disparando = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    public void DispararAlJugador(Transform objetivo)
    {
        if (!disparando && objetivo != null)
        {
            jugador = objetivo;
            StartCoroutine(RutinaDisparoLaser());
        }
    }

    private IEnumerator RutinaDisparoLaser()
    {
        disparando = true;
        lineRenderer.enabled = true;

        // 1. FASE DE ADVERTENCIA (Sigue al jugador)
        float t = 0f;
        lineRenderer.startWidth = grosorAdvertencia;
        lineRenderer.endWidth = grosorAdvertencia;
        lineRenderer.startColor = colorAdvertencia;
        lineRenderer.endColor = colorAdvertencia;

        Vector3 direccionFija = Vector3.up;

        while (t < tiempoAdvertencia)
        {
            t += Time.deltaTime;

            if (jugador != null)
            {
                // Guarda la dirección hacia donde estaba el jugador
                direccionFija = (jugador.position - transform.position).normalized;
            }

            Vector3 origen = transform.position;
            Vector3 destino = origen + (direccionFija * distanciaMax);

            lineRenderer.SetPosition(0, origen);
            lineRenderer.SetPosition(1, destino);

            yield return null;
        }

        // 2. FASE DE DISPARO REAL (Rayos fijos en esa dirección)
        // 2. FASE DE DISPARO REAL (Rayo cargado)
        lineRenderer.startWidth = grosorDisparo;
        lineRenderer.endWidth = grosorDisparo;
        lineRenderer.startColor = colorDisparo;
        lineRenderer.endColor = colorDisparo;

        float tiempoDisparo = 0f;
        Vector3 puntoOrigen = transform.position;
        Vector3 puntoDestino = puntoOrigen + (direccionFija * distanciaMax);

        // Controlamos para que el daño no se aplique a cada frame flotante, sino en enteros
        float danoAcumulado = 0f;

        while (tiempoDisparo < duracionDisparo)
        {
            tiempoDisparo += Time.deltaTime;

            // Detección de colisión usando Raycast2D
            RaycastHit2D hit = Physics2D.Raycast(puntoOrigen, direccionFija, distanciaMax);
            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                danoAcumulado += danoPorSegundo * Time.deltaTime;

                // Convertimos el daño flotante acumulado a un entero válido
                int danoEntero = Mathf.FloorToInt(danoAcumulado);

                if (danoEntero >= 1)
                {
                    // Llama exactamente a tu método RecibirDaño(int cantidad)
                    hit.collider.SendMessage("RecibirDaño", danoEntero, SendMessageOptions.DontRequireReceiver);
                    danoAcumulado -= danoEntero; // Resta la parte entera ya enviada
                }
            }

            yield return null;
        }

        // 3. APAGADO
        lineRenderer.enabled = false;
        disparando = false;
    }

    public bool EstaDisparando()
    {
        return disparando;
    }
}