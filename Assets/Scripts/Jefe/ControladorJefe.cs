using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ControladorJefe : MonoBehaviour
{
    [Header("Estadísticas")]
    public float vida = 50f;
    public float velocidad = 2f;
    public float rangoAtaque = 10f;
    public float rangoDetencion = 4f;

    [Header("Disparo")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float dañoProyectil = 2f;

    [Header("Secuencia de Patrones (orden y delays)")]
    public List<EntradaPatron> patrones = new List<EntradaPatron>();
    public bool repetirCiclo = true;
    public float esperaInicial = 0.5f;

    [Header("Lluvia de fondo (paralela a cada patrón)")]
    public bool lluviaActivada = true;
    public int lluviaBalasPorAnillo = 10;
    public float lluviaIntervalo = 0.35f;
    public OpcionesProyectil lluviaOpciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 4.95f,
        tiempoVuelo = 3f,
        color = new Color(0.7f, 0.2f, 1f, 0.7f)
    };

    // --- Estado interno ---
    private bool estaMuerto = false;
    private bool estaAtacando = false;
    private bool mirandoDerecha = true;
    private bool estaGolpeado = false;
    private bool lluviaActiva = false;
    public bool esInvulnerable = false;

    private Transform jugador;
    private Rigidbody2D rb;
    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        SetAnim("IsMoving", false);
        SetAnim("IsAttacking", false);
        SetAnim("Hit", false);
        SetAnim("IsDead", false);

        var jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null) jugador = jugadorObj.transform;
    }

    void SetAnim(string p, bool v) { if (anim != null) anim.SetBool(p, v); }

    void FixedUpdate()
    {
        if (estaMuerto || estaGolpeado || jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia < rangoAtaque)
        {
            bool jugadorALaDerecha = jugador.position.x > transform.position.x;
            if (jugadorALaDerecha && !mirandoDerecha) Girar();
            else if (!jugadorALaDerecha && mirandoDerecha) Girar();

            if (distancia > rangoDetencion && !estaAtacando)
            {
                float dir = mirandoDerecha ? -1f : 1f;
                rb.linearVelocity = new Vector2(dir * velocidad, rb.linearVelocity.y);
                SetAnim("IsMoving", true);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                SetAnim("IsMoving", false);
                if (!estaAtacando) StartCoroutine(RutinaAtaque());
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnim("IsMoving", false);
        }
    }

    IEnumerator RutinaAtaque()
    {
        estaAtacando = true;
        esInvulnerable = true;
        SetAnim("IsAttacking", true);
        SetAnim("Hit", false);

        yield return new WaitForSeconds(esperaInicial);
        esInvulnerable = false;

        if (patrones == null || patrones.Count == 0)
        {
            SetAnim("IsAttacking", false);
            estaAtacando = false;
            yield break;
        }

        int idx = 0;
        while (true)
        {
            var entrada = patrones[idx];
            if (entrada != null)
            {
                if (entrada.retardoAntes > 0f)
                    yield return new WaitForSeconds(entrada.retardoAntes);

                if (entrada.patron != null)
                {
                    var ctx = new ContextoPatron
                    {
                        jefe = this,
                        puntoDisparo = puntoDisparo != null ? puntoDisparo : transform,
                        jugador = jugador
                    };

                    if (lluviaActivada) StartCoroutine(LluviaFondo(ctx));

                    yield return StartCoroutine(entrada.patron.Ejecutar(ctx));

                    lluviaActiva = false; // detener lluvia de fondo
                }

                if (entrada.retardoDespues > 0f)
                    yield return new WaitForSeconds(entrada.retardoDespues);
            }

            idx++;
            if (idx >= patrones.Count)
            {
                if (repetirCiclo) idx = 0;
                else break;
            }
        }

        SetAnim("IsAttacking", false);
        estaAtacando = false;
    }

    IEnumerator LluviaFondo(ContextoPatron ctx)
    {
        lluviaActiva = true;
        float angulo = 0f;
        while (lluviaActiva)
        {
            ctx.DispararCirculo(lluviaBalasPorAnillo, angulo, lluviaOpciones);
            angulo += 17f;
            yield return new WaitForSeconds(lluviaIntervalo);
        }
    }

    public void InstanciarProyectil(Transform origen, Vector2 direccion, OpcionesProyectil opciones)
    {
        if (prefabProyectil == null) return;
        Vector3 pos = origen != null ? origen.position : transform.position;
        GameObject go = Instantiate(prefabProyectil, pos, Quaternion.identity);
        var p = go.GetComponent<ProyectilJefe>();
        if (p != null) p.Configurar(direccion.normalized, opciones, dañoProyectil);

        float rot = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        var e = transform.localScale; e.x *= -1; transform.localScale = e;
    }

    public void AplicarDaño(float daño)
    {
        if (esInvulnerable || estaMuerto) return;

        float dir = daño / Mathf.Abs(daño);
        daño = Mathf.Abs(daño);
        vida -= daño;

        if (vida <= 0) Morir();
        else if (estaAtacando) StartCoroutine(ParpadeoDaño());
        else
        {
            SetAnim("Hit", true);
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(dir * 400f, 100f));
            StartCoroutine(TiempoGolpe());
        }
    }

    IEnumerator ParpadeoDaño()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            sr.color = original;
        }
    }

    void Morir()
    {
        estaMuerto = true;
        SetAnim("IsDead", true);
        rb.linearVelocity = Vector2.zero;
        estaAtacando = false;
        StopAllCoroutines();
        StartCoroutine(DestruirJefe());
    }

    IEnumerator TiempoGolpe()
    {
        estaGolpeado = true;
        esInvulnerable = true;
        yield return new WaitForSeconds(0.15f);
        estaGolpeado = false;
        esInvulnerable = false;
        SetAnim("Hit", false);
    }

    IEnumerator DestruirJefe()
    {
        var capsula = GetComponent<CapsuleCollider2D>();
        if (capsula != null)
        {
            capsula.size = new Vector2(1f, 0.25f);
            capsula.offset = new Vector2(0f, -0.8f);
            capsula.direction = CapsuleDirection2D.Horizontal;
        }
        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    void OnCollisionStay2D(Collision2D colision)
    {
        if (colision.gameObject.CompareTag("Player") && !estaMuerto)
            colision.gameObject.GetComponent<PlayerShip2D>()?.ApplyDamage(2f);
    }
}