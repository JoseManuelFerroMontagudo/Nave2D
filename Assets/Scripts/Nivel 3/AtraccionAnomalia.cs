using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AtraccionAnomalia : MonoBehaviour
{
    private Transform centroAnomalia;
    private float fuerzaSuccion = 8f;
    private bool activo = false;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void IniciarSuccion(Transform destino, float velocidad)
    {
        centroAnomalia = destino;
        fuerzaSuccion = velocidad;
        activo = true;

        // Cancelamos la velocidad lineal que le asignó el script 'Asteroide' original
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (!activo || centroAnomalia == null) return;

        // 1. Mover hacia el centro de la anomalía
        transform.position = Vector3.MoveTowards(transform.position, centroAnomalia.position, fuerzaSuccion * Time.deltaTime);

        // 2. Encoger el asteroide a medida que entra al agujero negro
        float distancia = Vector2.Distance(transform.position, centroAnomalia.position);
        float escala = Mathf.Clamp01(distancia / 6f); // A menos de 6 unidades empieza a achicarse
        transform.localScale = Vector3.one * escala;

        // 3. Al ser completamente devorado por la singularidad
        if (distancia < 0.3f)
        {
            // Reseteamos su escala original por si vuelve a usarse en Object Pooling
            transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        activo = false;
        transform.localScale = Vector3.one;
    }
}