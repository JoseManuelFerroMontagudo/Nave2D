using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Sierra : MonoBehaviour, IDamageDealer
{
    public float multiplicadorBoost = 2f;
    public float velocidadEscala = 10f;

    [Tooltip("Lo lee el destino al colisionar, igual que Projectile.daño")]
    public float daño;
    public float Daño => daño;

    // Para que ShipInteractions sepa si bloquea proyectiles
    public bool IsBoosted => boost;

    float dañoBase, escalaBase;
    bool boost;

    public void Initialize(float d, float e)
    {
        dañoBase = d;
        escalaBase = e;
        daño = d * 0.5f;
        transform.localScale = Vector3.one * e;
    }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        gameObject.tag = "Sierra";
    }

    void Update()
    {
        float obj = escalaBase * (boost ? multiplicadorBoost : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * obj, velocidadEscala * Time.deltaTime);
    }

    public void SetBoost(bool b)
    {
        boost = b;
        daño = dañoBase * (b ? 1f : 0.5f);
    }

    // === Bloqueo de proyectiles solo en boost ===
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!boost) return;

        if (otro.CompareTag("Bullet") ||
            otro.CompareTag("Bullet1") ||
            otro.CompareTag("Bullet2") ||
            otro.CompareTag("Bullet3"))
        {
            Destroy(otro.transform.root.gameObject);
        }
        // Los asteroides los sigue gestionando el casco (comportamiento original).
    }
}