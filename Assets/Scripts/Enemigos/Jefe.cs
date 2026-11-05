using UnityEngine;

public class Jefe : Enemigo
{
    [Header("Patrones por fase (SO)")]
    public AtaqueBase[] patronesPorFase;

    // ❌ ELIMINAR: VidaPersonaje vida;

    protected override void Awake()
    {
        base.Awake();                          // aquí ya se asigna 'vida' heredado
        if (vida != null) vida.OnFaseCambia.AddListener(AlCambiarFase);
        AplicarFaseActual();
    }

    void AlCambiarFase(int nuevaFase) => AplicarFaseActual();

    void AplicarFaseActual()
    {
        if (patronesPorFase == null || patronesPorFase.Length == 0) return;
        int idx = Mathf.Clamp((vida != null ? vida.faseActual : 1) - 1, 0, patronesPorFase.Length - 1);
        var p = patronesPorFase[idx];
        if (p != null) ataque = p;
    }

    protected override void OnDestroy()
    {
        if (vida != null) vida.OnFaseCambia.RemoveListener(AlCambiarFase);
        base.OnDestroy();   // ← importante: quita también DesactivarAtaque de Enemigo
    }
}