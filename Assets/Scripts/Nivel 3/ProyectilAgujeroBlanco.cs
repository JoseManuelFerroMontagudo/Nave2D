using UnityEngine;

public class ProyectilAgujeroBlanco : MonoBehaviour
{
    public int dano = 15;
    public float tiempoVida = 6f;

    private Vector2 direccion;
    private float velocidad;
    private float velocidadAngular; // Para balas que hacen espirales curvas

    public void Configurar(Vector2 dir, float vel, float giroGradosPorSeg = 0f, float escala = 1f, Color? tinte = null)
    {
        direccion = dir.normalized;
        velocidad = vel;
        velocidadAngular = giroGradosPorSeg;
        transform.localScale = transform.localScale * escala;

        if (tinte.HasValue)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = tinte.Value;
        }

        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        if (velocidadAngular != 0f)
        {
            direccion = Quaternion.Euler(0, 0, velocidadAngular * Time.deltaTime) * direccion;
        }

        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);

        // Rotar el sprite hacia donde avanza
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            other.transform.root.BroadcastMessage("RecibirDaño", dano, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }
    }
}