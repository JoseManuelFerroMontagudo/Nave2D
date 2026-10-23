using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    [Header("Jerarquía (hijo que apunta)")]
    public Transform hijoAim;
    public Transform puntoDisparo;

    [Header("Anclaje")]
    [HideInInspector] public Vector3 puntoAncla;
    [HideInInspector] public bool enPosicion = true;   // true por defecto (colocados a mano)
    public float radioAnclaje = 2f;                    // límite de la órbita del animator

    [Header("Ataque (SO compartido)")]
    public PatronAtaque patron;
    [Tooltip("Solo se usa si NO hay Animator en el padre")]
    public float cadencia = 3f;

    protected Animator anim;
    protected Transform jugador;
    protected float cronometro;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) jugador = p.transform;
    }

    protected virtual void Update()
    {
        // Aim: el hijo siempre apunta al jugador
        if (jugador != null && hijoAim != null)
        {
            Vector2 d = (jugador.position - hijoAim.position).normalized;
            hijoAim.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        if (!enPosicion) return;

        // Movimiento: si hay Animator, lo hace él alrededor del ancla (root motion / clips).
        // Si NO hay Animator, el enemigo se queda quieto y dispara por tiempo.
        if (anim == null && patron != null)
        {
            cronometro += Time.deltaTime;
            if (cronometro >= cadencia)
            {
                cronometro = 0f;
                Disparar();
            }
        }
    }

    // Clamp del radio de anclaje: si el animator/root motion intenta salir, lo frena.
    protected virtual void LateUpdate()
    {
        if (!enPosicion || anim == null) return;

        Vector3 offset = transform.position - puntoAncla;
        if (offset.sqrMagnitude > radioAnclaje * radioAnclaje)
            transform.position = puntoAncla + offset.normalized * radioAnclaje;
    }

    // ----- Animation Events (solo con Animator) -----
    public virtual void EventoDisparo() => Disparar();
    public virtual void EventoFinAtaque() { /* hook opcional */ }

    public virtual void Disparar()
    {
        if (patron == null || puntoDisparo == null || jugador == null) return;
        StartCoroutine(patron.Ejecutar(puntoDisparo, jugador));
    }
}