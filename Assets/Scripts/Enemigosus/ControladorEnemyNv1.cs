using UnityEngine;
using System.Collections;

public class ControladorEnemyNv1 : MonoBehaviour
{
    [Header("Estadísticas de la Nave")]
    public float vida = 10f;
    public float velocidadMovimiento = 3f;
    public float rangoAtaque = 6f;

    [Header("Referencias Visuales 2.5D (Sprites del 0 al 7)")]
    public Sprite[] spritesDirecciones;
    private SpriteRenderer spriteRenderer;

    [Header("Configuración de Disparos de Grupo")]
    public GameObject prefabProyectilCustom;
    public Transform puntoDisparo;
    public float dañoProyectilCustom = 1f;
    public float cadenciaAtaque = 3f;

    [Header("Patrones de Ataque (ScriptableObjects)")]
    public PatronBase patronRafaga;
    public PatronBase patronCono;

    public enum TipoAtaqueActual { RafagaDirecta, ConoAbanico }
    [Header("Comportamiento Activo")]
    public TipoAtaqueActual ataqueActivo = TipoAtaqueActual.RafagaDirecta;

    private Transform jugador;
    private float cronometroAtaque;
    private bool estaAtacando = false;
    private ControladorJefe simuladorJefe;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        simuladorJefe = GetComponent<ControladorJefe>();

        var jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null) jugador = jugadorObj.transform;
    }

    void Start()
    {
        if (simuladorJefe != null)
        {
            simuladorJefe.prefabProyectil = prefabProyectilCustom;
            simuladorJefe.puntoDisparo = puntoDisparo;
            simuladorJefe.dañoProyectil = dañoProyectilCustom;
            simuladorJefe.velocidadMovimiento = 0f;
            simuladorJefe.lluviaActivada = false;
            simuladorJefe.enabled = false;
        }
    }

    void Update()
    {
        if (jugador == null || estaAtacando) return;

        Vector2 direccionAlJugador = (jugador.position - transform.position).normalized;
        float distanciaAlJugador = Vector2.Distance(transform.position, jugador.position);

        GestionarSpriteIsometrice(direccionAlJugador);

        if (distanciaAlJugador > rangoAtaque)
        {
            transform.position = Vector2.MoveTowards(transform.position, jugador.position, velocidadMovimiento * Time.deltaTime);
        }

        cronometroAtaque += Time.deltaTime;
        if (distanciaAlJugador <= rangoAtaque && cronometroAtaque >= cadenciaAtaque)
        {
            cronometroAtaque = 0f;
            StartCoroutine(EjecutarCicloAtaque(direccionAlJugador));
        }

    }

    void GestionarSpriteIsometrice(Vector2 dir)
    {
        if (spritesDirecciones == null || spritesDirecciones.Length < 8 || spriteRenderer == null) return;

        spriteRenderer.sprite = spritesDirecciones[4]; // siempre el sprite bonito

        // Rotar el Transform para que el morro (que apunta hacia abajo en el sprite 4)
        // apunte al jugador
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // +90 porque el sprite 4 mira hacia abajo (-Y), y queremos alinearlo con dir
        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angulo + 90f);
    }

    IEnumerator EjecutarCicloAtaque(Vector2 dir)
    {
        if (simuladorJefe == null) yield break;
        estaAtacando = true;

        var ctx = new ContextoPatron
        {
            jefe = simuladorJefe,
            puntoDisparo = puntoDisparo != null ? puntoDisparo : transform,
            jugador = jugador
        };

        PatronBase patronElegido = (ataqueActivo == TipoAtaqueActual.RafagaDirecta) ? patronRafaga : patronCono;

        if (patronElegido != null)
        {
            float velOriginal = velocidadMovimiento;
            if (ataqueActivo == TipoAtaqueActual.RafagaDirecta) velocidadMovimiento = 2f;

            yield return StartCoroutine(patronElegido.Ejecutar(ctx));

            velocidadMovimiento = velOriginal;
        }

        yield return new WaitForSeconds(0.3f);
        estaAtacando = false;
    }

    public void TakeDamage(float daño) => AplicarDaño(daño);

    public void AplicarDaño(float daño)
    {
        vida -= Mathf.Abs(daño);
        if (vida <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
