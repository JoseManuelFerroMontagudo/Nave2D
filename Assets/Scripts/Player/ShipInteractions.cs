using UnityEngine;

[RequireComponent(typeof(Stats))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Daño por proyectil")]
    public string etiquetaBala = "Bullet";
    public int dañoBala = 5;

    [Header("Daño por contacto")]
    public string etiquetaEnemigo = "Enemy";
    public int dañoEnemigo = 10;
    public float intervaloDañoContacto = 0.5f;

    [Header("Recogidas")]
    public string etiquetaBotiquin = "HealthPickup";
    public int curacionBotiquin = 25;
    public string etiquetaMunicion = "AmmoPickup";
    public int cantidadMunicion = 10;
    public string etiquetaEnergia = "EnergyPickup";
    public int cantidadEnergia = 25;

    Stats estadisticas;
    float siguienteDañoContactoPermitido;

    void Awake() => estadisticas = GetComponent<Stats>();

    // ==================== TRIGGERS ====================
    // Balas enemigas y objetos recogibles
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag(etiquetaBala))
        {
            estadisticas.RecibirDaño(dañoBala);
            Destroy(otro.gameObject);
            return;
        }

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
    }

    // ==================== COLISIONES ====================
    void OnCollisionStay2D(Collision2D choque)
    {
        if (!choque.collider.CompareTag(etiquetaEnemigo)) return;
        if (Time.time < siguienteDañoContactoPermitido) return;

        siguienteDañoContactoPermitido = Time.time + intervaloDañoContacto;
        estadisticas.RecibirDaño(dañoEnemigo);
    }
}