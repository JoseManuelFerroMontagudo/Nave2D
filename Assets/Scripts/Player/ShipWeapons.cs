using UnityEngine;
using UnityEngine.InputSystem;

public class ShipWeapons : MonoBehaviour
{
    [Header("Modo Arsenal (player)")]
    public ArsenalJugador arsenalJugador;

    [Header("Modo Directo (torreta / sin arsenal)")]
    [Tooltip("Solo se usan si arsenalJugador es null")]
    public AtaqueBase ataquePrimario;
    public AtaqueBase ataqueSecundario;

    [Header("Config")]
    public bool autoDisparoPrimario = false;

    [Header("Torreta (opcional)")]
    [Tooltip("Objetivo para orientar disparos. Solo aplica si arsenalJugador es null.")]
    public Transform objetivo;

    AtaqueRunner runnerPrimario;
    AtaqueRunner runnerSecundario;
    Stats stats;
    AtaqueBase ultimoPrim, ultimoSec;

    void Awake()
    {
        stats = GetComponent<Stats>();
        runnerPrimario = new AtaqueRunner(this);
        runnerSecundario = new AtaqueRunner(this);

        Configurar(runnerPrimario, autoDisparoPrimario);
        Configurar(runnerSecundario, false);

        SincronizarDesdeArsenal();
    }

    void Configurar(AtaqueRunner r, bool auto)
    {
        r.owner = transform;
        r.origen = null;
        r.objetivo = objetivo;
        r.stats = stats;
        r.auto = auto;
    }

    public void OnPrimaryAttack(InputValue v) => runnerPrimario.SetHeld(v.isPressed);
    public void OnSecondaryAttack(InputValue v) => runnerSecundario.SetHeld(v.isPressed);

    void Update()
    {
        if (arsenalJugador != null &&
            (ultimoPrim != arsenalJugador.equipadaPrimaria ||
             ultimoSec != arsenalJugador.equipadaSecundaria))
            SincronizarDesdeArsenal();

        if (arsenalJugador == null &&
            (ultimoPrim != ataquePrimario || ultimoSec != ataqueSecundario))
            SincronizarDesdeArsenal();

        runnerPrimario.Tick(Time.deltaTime);
        runnerSecundario.Tick(Time.deltaTime);
    }

    public void SincronizarDesdeArsenal()
    {
        if (arsenalJugador != null)
        {
            arsenalJugador.ReconciliarEquipados();
            ultimoPrim = arsenalJugador.equipadaPrimaria;
            ultimoSec = arsenalJugador.equipadaSecundaria;
        }
        else
        {
            ultimoPrim = ataquePrimario;
            ultimoSec = ataqueSecundario;
        }
        runnerPrimario.SetAtaque(ultimoPrim);
        runnerSecundario.SetAtaque(ultimoSec);
        runnerPrimario.auto = autoDisparoPrimario;
    }

    public void OnSwap()
    {
        if (arsenalJugador == null) return;
        (arsenalJugador.equipadaPrimaria, arsenalJugador.equipadaSecundaria) =
        (arsenalJugador.equipadaSecundaria, arsenalJugador.equipadaPrimaria);
    }
}