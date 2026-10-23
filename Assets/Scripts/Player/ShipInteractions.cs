using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Referencias")]
    public Stats estadisticas;
    public ShieldController escudo;

    [Header("Daño por proyectil")]
    public int dañoBullet1 = 1;
    public int dañoBullet2 = 5;
    public int dañoBullet3 = 10;

    [Header("Daño por contacto")]
    public float intervaloDañoContacto = 0.5f;

    [Header("Recogidas")]
    public string etiquetaBotiquin  = "HealthPickup";
    public int    curacionBotiquin  = 25;
    public string etiquetaMunicion  = "AmmoPickup";
    public int    cantidadMunicion  = 10;
    public string etiquetaEnergia   = "EnergyPickup";
    public int    cantidadEnergia   = 25;

    float siguienteDañoContactoPermitido;

    void Awake()
    {
        // Stats y escudo viven en el padre (Ship), no en Hull
        if (estadisticas == null) estadisticas = GetComponentInParent<Stats>();
        if (escudo == null)       escudo       = GetComponentInParent<ShieldController>();
    }

    // ==================== TRIGGERS (balas y recogidas) ====================
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Bullet"))  { ProcesarBala(otro, dañoBullet1); return; }
        if (otro.CompareTag("Bullet2")) { ProcesarBala(otro, dañoBullet2); return; }
        if (otro.CompareTag("Bullet3")) { ProcesarBala(otro, dañoBullet3); return; }

        if (otro.CompareTag(etiquetaBotiquin)) { estadisticas.Curar(curacionBotiquin);          Destruir(otro); return; }
        if (otro.CompareTag(etiquetaMunicion)) { estadisticas.AñadirMunicion(cantidadMunicion); Destruir(otro); return; }
        if (otro.CompareTag(etiquetaEnergia))  { estadisticas.AñadirEnergia(cantidadEnergia);   Destruir(otro); return; }
    }

    void ProcesarBala(Collider2D bala, int daño)
    {
        // Edge case: si el escudo está activo y la bala se destruye en el mismo frame,
        // el evento puede llegar también al hull antes del Destroy diferido.
        if (escudo != null && escudo.EstaActivo) return;

        estadisticas.RecibirDaño(daño);
        Destruir(bala);
    }

    // ==================== COLISIONES (no trigger: enemigos, asteroides) ====================
    void OnCollisionStay2D(Collision2D choque)
    {
        if (choque.collider.CompareTag("Enemy") || choque.collider.CompareTag("Boss"))
        {
            if (Time.time < siguienteDañoContactoPermitido) return;
            siguienteDañoContactoPermitido = Time.time + intervaloDañoContacto;
            estadisticas.RecibirDaño(20);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Asteroide"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            estadisticas.RecibirDaño(10);
        }
    }

    void Destruir(Collider2D c) => Destroy(c.transform.root.gameObject);
}