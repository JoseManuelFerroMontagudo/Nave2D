using UnityEngine;

[RequireComponent(typeof(Stats))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Colliders de la nave")]
    [Tooltip("Collider físico del casco (no trigger). Se usa para validar impactos.")]
    public Collider2D hullCollider;
    public ShieldController escudo;
    public Sierra sierra; // opcional: si la sierra bloquea balas en boost

    [Header("Daño por proyectil")]
    public int dañoBullet1 = 1;
    public int dañoBullet2 = 5;
    public int dañoBullet3 = 10;

    [Header("Daño por contacto")]
    public float intervaloDañoContacto = 0.5f;

    Stats estadisticas;
    float siguienteDañoContactoPermitido;

    void Awake()
    {
        estadisticas = GetComponent<Stats>();

        if (hullCollider == null)
        {
            // Fallback: primer Collider2D no-trigger en hijos (el casco)
            foreach (var c in GetComponentsInChildren<Collider2D>())
                if (!c.isTrigger) { hullCollider = c; break; }
        }
        if (escudo == null) escudo = GetComponentInChildren<ShieldController>();
        if (sierra == null) sierra = GetComponentInChildren<Sierra>();
    }

    // ==================== TRIGGERS ====================
    // Solo cuenta si la bala solapa el collider del CASCO.
    void OnTriggerEnter2D(Collider2D otro)
    {
        // Si el proyectil solo tocó escudo/sierra (no el casco), lo ignoramos:
        // ya lo gestiona ShieldController / Sierra.
        if (hullCollider != null && !otro.IsTouching(hullCollider)) return;

        // Escudo activo bloquea todo
        if (escudo != null && escudo.EstaActivo) return;

        // Sierra en boost bloquea proyectiles
        if (sierra != null && sierra.BloqueaProyectiles) return;

        if (otro.CompareTag("Bullet"))  { Dañar(dañoBullet1); DestruirBala(otro); return; }
        if (otro.CompareTag("Bullet2")) { Dañar(dañoBullet2); DestruirBala(otro); return; }
        if (otro.CompareTag("Bullet3")) { Dañar(dañoBullet3); DestruirBala(otro); return; }

        // (Opcional) Recogidas si quieres que el casco también las detecte
        // if (otro.CompareTag(etiquetaBotiquin)) { ... }
    }

    // ==================== COLISIONES (no trigger) ====================
    // Solo el casco es no-trigger, pero validamos por seguridad.
    void OnCollisionStay2D(Collision2D choque)
    {
        if (hullCollider != null && choque.otherCollider != hullCollider) return;

        if (choque.collider.CompareTag("Enemy") || choque.collider.CompareTag("Boss"))
        {
            if (Time.time < siguienteDañoContactoPermitido) return;
            siguienteDañoContactoPermitido = Time.time + intervaloDañoContacto;
            Dañar(20);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (hullCollider != null && col.otherCollider != hullCollider) return;

        if (col.collider.CompareTag("Asteroide"))
        {
            if (escudo != null && escudo.EstaActivo) return;
            Dañar(10);
        }
    }

    void Dañar(int cantidad)
    {
        if (estadisticas != null) estadisticas.RecibirDaño(cantidad);
    }

    void DestruirBala(Collider2D bala)
    {
        // Por si el prefab tiene el collider en un hijo
        Destroy(bala.transform.root.gameObject);
    }
}