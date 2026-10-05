using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Común")]
    public float vidaUtil = 3f;
    public GameObject prefabEfectoImpacto;

    [Header("Persecución (giroPorSegundo = 0 → bala recta)")]
    public float giroPorSegundo = 0f;    // grados/segundo
    public float radioDeteccion = 6f;
    public LayerMask capasObjetivo = ~0;
    public string etiquetaObjetivo = "Enemy";

    float angulo;         // dirección en grados (sustituye al Vector2 dirección)
    float velocidad;
    float edad;
    Transform objetivo;

    public void Initialize(Vector2 velocidadInicial, Transform objetivoExplicito = null)
    {
        angulo = velocidadInicial.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(velocidadInicial.y, velocidadInicial.x) * Mathf.Rad2Deg
            : transform.eulerAngles.z + 90f;

        velocidad = velocidadInicial.magnitude;
        edad = 0f;
        objetivo = objetivoExplicito;
        Destroy(gameObject, vidaUtil);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        edad += dt;

        if (giroPorSegundo > 0f)
        {
            if (objetivo == null) objetivo = BuscarObjetivoCercano();

            if (objetivo != null)
            {
                float giroActual = Mathf.Min(giroPorSegundo + 30f * edad, 270f);
                Vector2 haciaObjetivo = (Vector2)objetivo.position - (Vector2)transform.position;
                float anguloObjetivo = Mathf.Atan2(haciaObjetivo.y, haciaObjetivo.x) * Mathf.Rad2Deg;
                angulo += Mathf.Clamp(
                    Mathf.DeltaAngle(angulo, anguloObjetivo),
                    -giroActual * dt, giroActual * dt);
            }
        }

        float rad = angulo * Mathf.Deg2Rad;
        Vector2 direccion = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        transform.position += (Vector3)(direccion * velocidad * dt);
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);
    }

    Transform BuscarObjetivoCercano()
    {
        var impactos = Physics2D.OverlapCircleAll(transform.position, radioDeteccion, capasObjetivo);
        float mejorDist = float.MaxValue;
        Transform mejor = null;

        foreach (var h in impactos)
        {
            if (!h.CompareTag(etiquetaObjetivo)) continue;
            float d = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < mejorDist) { mejorDist = d; mejor = h.transform; }
        }
        return mejor;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag(etiquetaObjetivo)) return;

        if (prefabEfectoImpacto != null)
            Instantiate(prefabEfectoImpacto, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}