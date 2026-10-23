using UnityEngine;

public class Jefe : Enemigo
{
    [Header("Patrones por fase (SO)")]
    public PatronAtaque[] patronesPorFase;

    VidaPersonaje vida;

    protected override void Awake()
    {
        base.Awake();
        vida = GetComponent<VidaPersonaje>();
        if (vida != null)
            vida.OnFaseCambia.AddListener(AlCambiarFase);
    }

    void AlCambiarFase(int nuevaFase)
    {
        cronometro = 0f;                       // reinicia el timer (modo sin Animator)
        // aquí podrías: anim.SetTrigger("PhaseChange");  etc.
    }

    // Sobrescribe Disparar (no EventoDisparo): así cubre
    // tanto la ruta Animation Event como la ruta timer.
    public override void Disparar()
    {
        if (jugador == null || puntoDisparo == null) return;
        if (patronesPorFase == null || patronesPorFase.Length == 0) return;

        int idx = Mathf.Clamp((vida != null ? vida.faseActual : 1) - 1, 0, patronesPorFase.Length - 1);
        var p = patronesPorFase[idx];
        if (p == null) return;

        StartCoroutine(p.Ejecutar(puntoDisparo, jugador));
    }
}