using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IDamageDealer
{
    public float vidaUtil = 3f;
    public int daño = 10;
    public int Daño => daño;
    public GameObject prefabEfectoImpacto;
    [Header("Comportamiento")]
    public bool destruirAlImpactar = true;
    [Header("Persecución (0 = recto)")]
    public float giroPorSegundo = 0f;
    public float radioDeteccion = 6f;
    public LayerMask capasObjetivo = ~0;
    public string[] etiquetasObjetivo = { "Enemy", "Boss" };
    public string[] etiquetasIgnoradas = { "Player", "Shield", "Bullet", "Bullet1", "Bullet2", "Bullet3", "Asteroide", "Sierra" };

    float angulo, velocidad, edad;
    Transform objetivo;

    public void Initialize(Vector2 velInicial, Transform objetivoExplicito = null)
    {
        angulo = velInicial.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(velInicial.y, velInicial.x) * Mathf.Rad2Deg
            : transform.eulerAngles.z + 90f;
        velocidad = velInicial.magnitude;
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
                float giro = Mathf.Min(giroPorSegundo + 30f * edad, 270f);
                Vector2 hacia = (Vector2)objetivo.position - (Vector2)transform.position;
                float ang = Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;
                angulo += Mathf.Clamp(Mathf.DeltaAngle(angulo, ang), -giro * dt, giro * dt);
            }
        }

        float rad = angulo * Mathf.Deg2Rad;
        transform.position += (Vector3)(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * velocidad * dt);
        transform.rotation = Quaternion.Euler(0, 0, angulo - 90f);
    }

    Transform BuscarObjetivoCercano()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, radioDeteccion, capasObjetivo);
        float mejor = float.MaxValue;
        Transform r = null;
        foreach (var h in hits)
        {
            if (!Contiene(etiquetasObjetivo, h.tag)) continue;
            float d = ((Vector2)h.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < mejor) { mejor = d; r = h.transform; }
        }
        return r;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Contiene(etiquetasIgnoradas, otro.tag)) return;
        if (prefabEfectoImpacto) Instantiate(prefabEfectoImpacto, transform.position, transform.rotation);
        if (destruirAlImpactar) Destroy(gameObject);   // ← condicional
    }

    static bool Contiene(string[] arr, string t)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == t) return true;
        return false;
    }
}