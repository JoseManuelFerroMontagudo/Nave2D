using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class ProyectilJefe : MonoBehaviour
{
    public OpcionesProyectil opciones = new OpcionesProyectil();
    public Vector2 direccion;
    public float daño = 2f;

    private Rigidbody2D rb;
    private Transform jugador;
    private Vector3 posicionOrigen;
    private bool regresando = false;
    private bool frenando = false;
    private bool puedeGolpearParedes = false;
    private int rebotesRestantes;
    private float tiempoVivo = 0f;
    private float velocidadActual;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    public void Configurar(Vector2 dir, OpcionesProyectil opc, float dmg)
    {
        direccion = dir.normalized;
        opciones = opc;
        daño = dmg;
    }

    void Start()
    {
        if (rb == null) { Destroy(gameObject); return; }
        rb.gravityScale = 0f;
        posicionOrigen = transform.position;
        velocidadActual = opciones.velocidad;
        rebotesRestantes = opciones.rebotesMaximos;
        Destroy(gameObject, opciones.vidaUtil);
        StartCoroutine(ActivarColisionParedes());

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = opciones.color;

        var jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null) jugador = jugadorObj.transform;

        if (opciones.regresaAlOrigen) StartCoroutine(CicloDeVida());
    }

    IEnumerator ActivarColisionParedes()
    {
        yield return new WaitForSeconds(0.15f);
        puedeGolpearParedes = true;
    }

    IEnumerator CicloDeVida()
    {
        yield return new WaitForSeconds(opciones.tiempoVuelo);

        // Frenar
        frenando = true;
        float t = 0f;
        float velInicial = velocidadActual;
        while (t < opciones.tiempoFrenado)
        {
            velocidadActual = Mathf.Lerp(velInicial, 0f, t / opciones.tiempoFrenado);
            t += Time.deltaTime;
            yield return null;
        }
        velocidadActual = 0f;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(opciones.pausaAntesDeRegresar);

        // Regresar
        frenando = false;
        regresando = true;
        direccion = -direccion;
        puedeGolpearParedes = false;
        velocidadActual = opciones.velocidad * opciones.multiplicadorVelocidadRetorno;

        float rot = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        tiempoVivo += Time.fixedDeltaTime;

        if (frenando) { rb.linearVelocity = direccion.normalized * velocidadActual; return; }

        if (regresando && Vector2.Distance(transform.position, posicionOrigen) < 1f)
        {
            Destroy(gameObject);
            return;
        }

        switch (opciones.modo)
        {
            case ModoMovimiento.Recto:
            case ModoMovimiento.Desacelerar:
                rb.linearVelocity = direccion.normalized * velocidadActual;
                break;

            case ModoMovimiento.Ondulado:
                Vector2 baseVel = direccion.normalized * velocidadActual;
                Vector2 perp = new Vector2(-direccion.y, direccion.x).normalized;
                float onda = Mathf.Sin(tiempoVivo * opciones.frecuenciaOnda) * opciones.amplitudOnda;
                rb.linearVelocity = baseVel + perp * onda;
                break;

            case ModoMovimiento.Perseguidor:
                if (jugador != null && !regresando)
                {
                    Vector2 hacia = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
                    direccion = Vector2.Lerp(direccion, hacia, opciones.fuerzaPersecucion * Time.fixedDeltaTime).normalized;
                }
                rb.linearVelocity = direccion.normalized * velocidadActual;
                float ang = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, ang);
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player"))
        {
            var nave = otro.GetComponent<PlayerShip2D>();
            if (nave != null) nave.ApplyDamage(daño);
            Destroy(gameObject);
        }
        else if (otro.CompareTag("Enemy")) { /* ignorar */ }
        else if (puedeGolpearParedes && opciones.rebotaEnParedes && rebotesRestantes > 0)
        {
            rebotesRestantes--;
            direccion = -direccion;
            float rot = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, rot);
        }
    }
}