using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShipInteractions : MonoBehaviour
{
    [Header("Referencias")]
    public Stats estadisticas;
    public Shield escudo;

    [Header("Daño por contacto")]
    public int DañoContacto = 10;
    public float intervaloDañoContacto = 0.5f;

    [Header("Recogidas")]
    public int curacionBotiquin = 25;
    public int cantidadMunicion = 2;
    public int cantidadEnergia = 25;

    float siguienteDañoContactoPermitido;
    [Header("Dependencias")]
    [SerializeField] private ShipWeapons shipWeapons;

    private ArsenalJugador arsenal => shipWeapons != null ? shipWeapons.arsenalJugador : null;
    Sierra sierra;

    // -------- Detección de armas permanentes --------

    bool SierraBoosted
    {
        get
        {
            if (sierra == null) sierra = BuscarEnJerarquia<Sierra>();
            return sierra != null && sierra.IsBoosted;
        }
    }

    Shield EscudoActual
    {
        get
        {
            if (escudo == null) escudo = BuscarEnJerarquia<Shield>();
            return escudo;
        }
    }

    bool EscudoActivo => EscudoActual != null && EscudoActual.EstaActivo;

    /// Busca primero hacia arriba, luego hacia abajo desde la raíz, luego global.
    /// Así encuentra armas instanciadas como hijas de ShipWeapons.
    T BuscarEnJerarquia<T>() where T : Component
    {
        var t = GetComponentInParent<T>();
        if (t == null) t = transform.root.GetComponentInChildren<T>(true);
        if (t == null) t = FindAnyObjectByType<T>();
        return t;
    }

    void Awake()
    {
        if (estadisticas == null) estadisticas = GetComponentInParent<Stats>();
        if (escudo == null) escudo = BuscarEnJerarquia<Shield>();
        if (shipWeapons == null)
            shipWeapons = GetComponent<ShipWeapons>();

        if (shipWeapons == null || shipWeapons.arsenalJugador == null)
            Debug.LogError("[ShipInteractions] Falta ShipWeapons o su arsenal.", this);
    }

    // ============ TRIGGERS (ataques y recogidas) ============
    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Bullet"))
        {
            if (EscudoActivo) return;
            if (SierraBoosted) return;

            var d = otro.GetComponentInParent<IDamageDealer>();
            if (d != null)
            {
                estadisticas.RecibirDaño(d.Daño);
                Destroy(otro.transform.root.gameObject);
            }
            return;
        }

        if (otro.CompareTag("HealthPickup")) { estadisticas.Curar(curacionBotiquin); Destruir(otro); return; }
        if (otro.CompareTag("AmmoPickup"))
        {
            var item = otro.GetComponent<ItemRecogible>();
            if (item != null && !item.recogido)
            {
                item.recogido = true;
                RecogerArma(item);
            }
            Destruir(otro);
            return;
        }
        if (otro.CompareTag("EnergyPickup")) { estadisticas.AñadirEnergia(cantidadEnergia); Destruir(otro); return; }
    }

    // ============ COLISIONES (no-trigger) ============
    void OnCollisionStay2D(Collision2D choque)
    {
        // Contacto con enemigo: SOLO la Sierra protege (el Escudo no).
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
            if (EscudoActivo) return;
            if (SierraBoosted) return;
            estadisticas.RecibirDaño(DañoContacto);
        }
    }

    void Destruir(Collider2D c) => Destroy(c.transform.root.gameObject);
    void RecogerArma(ItemRecogible item)
    {
        if (item.armaAlmacenada == null) return;
        if (arsenal == null) { Debug.LogError("[Pickup] arsenal null"); return; }

        bool yaDesbloqueada = arsenal != null && arsenal.EstaDesbloqueada(item.armaAlmacenada);
        if (!yaDesbloqueada) arsenal.Desbloquear(item.armaAlmacenada);
        else estadisticas.AñadirPuntos(item.puntosSiDuplicada);

        if (item.esItemDeVictoria && item.prefabMenuVictoria != null)
            Instantiate(item.prefabMenuVictoria);
    }
}