using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour, IDamageDealer
{
    public int daño = 5;
    public int Daño => daño;
    public float velocidad = 6f;
    public float vidaUtil = 4f;

    [Header("Retorno al origen")]
    public bool volverAlOrigen = false;

    [Header("Trayectoria ondulada")]
    public float amplitudOnda = 0f;      // 0 = desactivado
    public float frecuenciaOnda = 0f;    // ciclos/segundo

    Vector3 origen, forward;
    float vidaTotal, t;

    public void Configurar(Vector2 dir) => forward = dir.normalized;

    void Start()
    {
        origen    = transform.position;
        vidaTotal = volverAlOrigen ? vidaUtil * 2f : vidaUtil;
        Destroy(gameObject, vidaTotal);
    }

    void Update()
    {
        t += Time.deltaTime;
        float mitad = vidaTotal * 0.5f;
        bool volviendo = volverAlOrigen && t > mitad;
        float tTramo = volviendo ? t - mitad : t;

        Vector3 dir = volviendo ? -forward : forward;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

        // avance recto desde el origen + offset perpendicular senoidal
        Vector3 pos = origen + dir * velocidad * tTramo;
        pos += perp * amplitudOnda * Mathf.Sin(2f * Mathf.PI * frecuenciaOnda * t);

        transform.position = pos;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player")) Destroy(gameObject);
    }
}