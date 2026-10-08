using UnityEngine;


[RequireComponent(typeof(Stats))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Daño por proyectil")]
    public string etiquetaBala = "Bullet";
    public int dañoBala = 5;

    [Header("Daño por contacto")]
    public float intervaloDañoContacto = 0.5f;

    [Header("Recogidas")]
    public string etiquetaBotiquin = "HealthPickup";
    public int curacionBotiquin = 25;
    public string etiquetaMunicion = "AmmoPickup";
    public int cantidadMunicion = 10;
    public string etiquetaEnergia = "EnergyPickup";
    public int cantidadEnergia = 25;

    ShieldController escudo;
    Stats estadisticas;
    float siguienteDañoContactoPermitido;

    void Awake()
    {
        estadisticas = GetComponent<Stats>();
        escudo = GetComponentInChildren<ShieldController>();
    }

    // ==================== TRIGGERS ====================
    // Balas enemigas y objetos recogibles
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Bullet"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            estadisticas.RecibirDaño(1);
            Destroy(otro.gameObject);
            return;
        }
        if (otro.CompareTag("Bullet2"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            estadisticas.RecibirDaño(5);
            Destroy(otro.gameObject);
            return;
        }
        if (otro.CompareTag("Bullet3"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            estadisticas.RecibirDaño(10);
            Destroy(otro.gameObject);
            return;
        }
        /*
                if (otro.CompareTag(etiquetaBotiquin))
                {
                    estadisticas.Curar(curacionBotiquin);
                    Destroy(otro.gameObject);
                    return;
                }

                if (otro.CompareTag(etiquetaMunicion))
                {
                    estadisticas.AñadirMunicion(cantidadMunicion);
                    Destroy(otro.gameObject);
                    return;
                }

                if (otro.CompareTag(etiquetaEnergia))
                {
                    estadisticas.AñadirEnergia(cantidadEnergia);
                    Destroy(otro.gameObject);
                }
                */
    }

    // ==================== COLISIONES ====================
    void OnCollisionStay2D(Collision2D choque)
    {
        if (choque.collider.CompareTag("Enemy") || choque.collider.CompareTag("Boss"))
        {
            if (Time.time < siguienteDañoContactoPermitido) return;

            siguienteDañoContactoPermitido = Time.time + intervaloDañoContacto;
            estadisticas.RecibirDaño(20);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        {
            if (collision.collider.CompareTag("Asteroide"))
            {
                if (escudo != null && escudo.EstaActivo) return;
                estadisticas.RecibirDaño(10);
                Destroy(collision.collider.gameObject);
            }
        }
    }
}