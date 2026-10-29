using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Referencias")]
    public Stats estadisticas;
    public ShieldController escudo;

    [Header("Daño por contacto")]
    public int DañoContacto = 10;
    public float intervaloDañoContacto = 0.5f;

    [Header("Recogidas")]
    public int curacionBotiquin = 25;
    public int cantidadMunicion = 2;
    public int cantidadEnergia = 25;

    float siguienteDañoContactoPermitido;
    Sierra sierra;
    bool SierraBoosted
    {
        get
        {
            if (sierra == null)
            {
                sierra = GetComponentInParent<Sierra>();
                if (sierra == null)
                    sierra = transform.root.GetComponentInChildren<Sierra>();
                if (sierra == null)
                    sierra = FindAnyObjectByType<Sierra>();
            }
            return sierra != null && sierra.IsBoosted;
        }
    }

    void Awake()
    {
        if (estadisticas == null) estadisticas = GetComponentInParent<Stats>();
        if (escudo == null) escudo = GetComponentInParent<ShieldController>();
    }

    // ============ TRIGGERS (ataques y recogidas) ============
    void OnTriggerEnter2D(Collider2D otro)
    {
        // --- Ataque con daño definido por IDamageDealer ---
        if (otro.CompareTag("Bullet"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            if (SierraBoosted) return;

            var d = otro.GetComponentInParent<IDamageDealer>();
            if (d != null)
            {
                estadisticas.RecibirDaño(d.Daño);
                Destroy(otro.transform.root.gameObject);
            }
            return;
        }

        // --- Recogidas ---
        if (otro.CompareTag("HealthPickup")) { estadisticas.Curar(curacionBotiquin); Destruir(otro); return; }
        if (otro.CompareTag("AmmoPickup")) { estadisticas.AñadirMunicion(cantidadMunicion); Destruir(otro); return; }
        if (otro.CompareTag("EnergyPickup")) { estadisticas.AñadirEnergia(cantidadEnergia); Destruir(otro); return; }
    }

    // ============ COLISIONES (no-trigger) ============
    void OnCollisionStay2D(Collision2D choque)
    {
        if (choque.collider.CompareTag("Enemy") || choque.collider.CompareTag("Boss"))
        {
            if (SierraBoosted) return;
            if (Time.time < siguienteDañoContactoPermitido) return;
            siguienteDañoContactoPermitido = Time.time + intervaloDañoContacto;
            estadisticas.RecibirDaño(DañoContacto);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Asteroid"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            if (SierraBoosted) return;
            estadisticas.RecibirDaño(DañoContacto);
        }
    }

    void Destruir(Collider2D c) => Destroy(c.transform.root.gameObject);
}