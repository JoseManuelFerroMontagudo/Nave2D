using UnityEngine;

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
    [Tooltip("Si NO hay Animator en el padre, dispara con la cadencia propia del ataque.")]
    public bool dispararSinAnimator = true;

    [Header("Estado")]
    [Tooltip("Si es false, no apunta, no dispara y no procesa el ataque.")]
    public bool puedeAtacar = true;

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

        // Hardpoint: si esta GO tiene VidaPersonaje, al morir se desactiva el ataque
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
        runner.SetAtaque(null); // cancela ráfaga + destruye permanente en curso
    }

    public virtual void ReactivarAtaque()
    {
        if (puedeAtacar) return;
        puedeAtacar = true;
        if (ataque != null) runner.SetAtaque(ataque);
    }

    // ───── Disparos externos (respetan puedeAtacar) ─────

    public void EventoDisparo() { if (puedeAtacar) runner.TryFire(true); }
    public void EventoFinAtaque() { }
    public virtual void Disparar() { if (puedeAtacar) runner.TryFire(); }

    protected virtual void OnDestroy()
    {
        if (vida != null) vida.OnMuere.RemoveListener(DesactivarAtaque);
    }
}