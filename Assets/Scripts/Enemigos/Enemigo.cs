using System.Collections;
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
    public PatronAtaque patron;
    [Tooltip("Solo se usa si NO hay Animator en el padre")]
    public float cadencia = 3f;

    // protected: accesibles desde subclases (Jefe)
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
        if (jugador != null && hijoAim != null)
        {
            Vector2 d = (jugador.position - hijoAim.position).normalized;
            hijoAim.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        if (!enPosicion) return;

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

    // Animation Events: deben ser public (o al menos no-private) para que Unity los encuentre
    public void EventoDisparo() => Disparar();
    public void EventoFinAtaque() { }

    public virtual void Disparar()
    {
        if (patron == null || puntoDisparo == null || jugador == null) return;
        StartCoroutine(patron.Ejecutar(puntoDisparo, jugador));
    }
}