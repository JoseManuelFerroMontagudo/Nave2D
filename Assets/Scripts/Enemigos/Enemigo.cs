using UnityEngine;
using System.Collections.Generic;

public class Enemigo : MonoBehaviour
{
    [Header("Jerarquía (hijo que apunta)")]
    public Transform hijoAim;
    public Transform puntoDisparo;

    [Header("Anclaje")]
    [HideInInspector] public Vector3 puntoAncla;
    [HideInInspector] public bool enPosicion = true;

    [Header("Ataque (SO compartido)")]
    public AtaqueBase ataque;
    public bool dispararSinAnimator = true;

    [Header("Estado")]
    public bool puedeAtacar = true;

    [Header("Drop al morir")]
    public TablaDrops tablaDrops;

    bool dropRealizado = false;

    protected Animator anim;
    protected Transform jugador;

    AtaqueRunner runner;
    AtaqueBase ataqueAnterior;
    protected VidaPersonaje vida;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) jugador = p.transform;

        runner = new AtaqueRunner(this)
        {
            owner = transform,
            origen = puntoDisparo,
            objetivo = jugador,
            stats = null
        };
        runner.SetAtaque(ataque);
        ataqueAnterior = ataque;

        vida = GetComponent<VidaPersonaje>();
        if (vida != null) vida.OnMuere.AddListener(DesactivarAtaque);
    }

    protected virtual void Update()
    {
        if (!puedeAtacar) return;

        if (jugador != null && hijoAim != null)
        {
            Vector2 d = (jugador.position - hijoAim.position).normalized;
            hijoAim.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        if (ataque != ataqueAnterior)
        {
            runner.SetAtaque(ataque);
            ataqueAnterior = ataque;
        }

        if (!enPosicion) { runner.Tick(Time.deltaTime); return; }

        if (anim == null && ataque != null && dispararSinAnimator)
            runner.TryFire();

        runner.Tick(Time.deltaTime);
    }

    // ───── API de activación ─────

    public virtual void DesactivarAtaque()
    {
        if (!puedeAtacar) return;
        puedeAtacar = false;
        runner.SetAtaque(null);
        SoltarDrops();
    }

    public virtual void ReactivarAtaque()
    {
        if (puedeAtacar) return;
        puedeAtacar = true;
        if (ataque != null) runner.SetAtaque(ataque);
    }

    // ───── Drop ─────

    protected virtual void SoltarDrops()
    {
        if (dropRealizado) return;
        dropRealizado = true;

        if (tablaDrops == null || tablaDrops.entradas == null) return;

        // 1) Armar lista: min garantizado (100%) + resto al 50%
        var lista = new List<GameObject>();
        foreach (var e in tablaDrops.entradas)
        {
            if (e == null || e.prefab == null) continue;

            int min = Mathf.Max(0, e.cantidadMin);
            int max = Mathf.Max(min, e.cantidadMax);

            for (int i = 0; i < min; i++) lista.Add(e.prefab);
            for (int i = min; i < max; i++)
                if (Random.value < 0.5f) lista.Add(e.prefab);
        }

        int n = lista.Count;
        if (n == 0) return;

        // 2) Ángulos equiespaciados barajados => direcciones únicas y aleatorias
        float[] angulos = new float[n];
        float paso = 360f / n;
        float offset = Random.Range(0f, 360f);

        for (int i = 0; i < n; i++) angulos[i] = offset + paso * i;

        // Fisher-Yates
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (angulos[i], angulos[j]) = (angulos[j], angulos[i]);
        }

        // 3) Instanciar en la posición exacta del enemigo y lanzar
        Vector3 origen = transform.position;

        for (int i = 0; i < n; i++)
        {
            float rad = angulos[i] * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            GameObject go = Instantiate(lista[i], origen, Quaternion.identity);

            if (go.TryGetComponent(out Rigidbody2D rbItem))
            {
                float fuerza = Random.Range(tablaDrops.fuerzaMin, tablaDrops.fuerzaMax);
                rbItem.linearVelocity = dir * fuerza; // Unity 2022-: rbItem.velocity
            }
        }
    }

    // ───── Disparos externos ─────
    public void EventoDisparo() { if (puedeAtacar) runner.TryFire(true); }
    public void EventoFinAtaque() { }
    public virtual void Disparar() { if (puedeAtacar) runner.TryFire(); }

    protected virtual void OnDestroy()
    {
        if (vida != null) vida.OnMuere.RemoveListener(DesactivarAtaque);
    }
}