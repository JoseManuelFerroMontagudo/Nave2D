using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Sierra : MonoBehaviour, IDamageDealer
{
    [Header("Sprites")]
    [SerializeField] Sprite spriteNormal;
    [SerializeField] Sprite spriteBoost;

    [Header("Config")]
    public int dañoBase = 100;
    public float escalaBase = 3f;
    public float multiplicadorBoost = 2f;
    public float velocidadEscala = 10f;

    public int daño;
    public int Daño => daño;
    public bool IsBoosted => boost;

    SpriteRenderer sr;
    bool boost;

    void Awake()
    {
        // Busca el SpriteRenderer en hijos (el que rota o cualquiera)
        sr = GetComponentInChildren<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        AplicarEstado();
    }

    public void SetBoost(bool b)
    {
        if (boost == b) return;
        boost = b;
        AplicarEstado();
    }

    void AplicarEstado()
    {
        daño = dañoBase * (boost ? 2 : 1);

        if (sr)
        {
            var s = boost ? spriteBoost : spriteNormal;
            if (s) sr.sprite = s;
        }
    }

    void Update()
    {
        float obj = escalaBase * (boost ? multiplicadorBoost : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * obj, velocidadEscala * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!boost) return;
        if (otro.CompareTag("Bullet"))
            Destroy(otro.transform.root.gameObject);
        if (otro.CompareTag("Asteroid"))
        {
            var asteroide = otro.GetComponentInParent<Asteroide>();
            if (asteroide != null)
                asteroide.Explotar();
        }
    }
}