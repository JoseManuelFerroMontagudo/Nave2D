using System.Collections;
using UnityEngine;

public class AtaqueRunner
{
    public AtaqueBase ataque;
    public Transform owner;
    public Transform origen;
    public Transform objetivo;
    public Stats stats;
    public bool auto;

    readonly MonoBehaviour host;

    bool held;
    float cooldown;
    AtaqueBase ataqueAnterior;
    GameObject instPermanente;

    bool enRafaga;                // ← ráfaga en curso
    Coroutine rafagaActual;       // ← handle para poder cancelarla

    public AtaqueRunner(MonoBehaviour host) { this.host = host; }

    public AtaqueBase AtaqueActual => ataque;
    public void SetHeld(bool v) => held = v;

    public void SetAtaque(AtaqueBase nuevo)
    {
        if (ataque == nuevo) return;
        DetenerRafaga();          // ← cancela también la ráfaga en curso
        DestruirPermanente();
        ataque = nuevo;
        ataqueAnterior = null;
        cooldown = 0f;
    }

    public bool TryFire(bool ignorarCooldown = false)
    {
        if (ataque == null || ataque.EsPermanente) return false;

        // ⭐ Si hay ráfaga en curso, NO la reiniciamos
        if (!ignorarCooldown && enRafaga) return false;
        if (!ignorarCooldown && Time.time < cooldown) return false;
        if (!PuedePagar()) return false;

        // Force-fire (p. ej. Animator) sí cancela la ráfaga anterior
        if (ignorarCooldown) DetenerRafaga();

        Pagar();
        cooldown = ataque.cadencia > 0.0001f ? Time.time + 1f / ataque.cadencia : Time.time;

        enRafaga = true;
        rafagaActual = host.StartCoroutine(RafagaWrapper());
        return true;
    }

    IEnumerator RafagaWrapper()
    {
        yield return ataque.Ejecutar(CrearContexto());
        enRafaga = false;
        rafagaActual = null;
    }

    void DetenerRafaga()
    {
        if (rafagaActual != null && host != null) host.StopCoroutine(rafagaActual);
        rafagaActual = null;
        enRafaga = false;
    }

    public void Tick(float dt)
    {
        if (ataque == null) return;
        if (ataque != ataqueAnterior) ActivarPermanente();
        if (ataque.EsPermanente) { ProcesarPermanente(); return; }
        if (auto || held) TryFire();
    }

    // ---- resto igual que antes ----
    void ActivarPermanente()
    {
        DestruirPermanente();
        ataqueAnterior = ataque;
        if (ataque != null && ataque.EsPermanente)
            instPermanente = ataque.Activar(CrearContexto());
    }

    void ProcesarPermanente()
    {
        bool boost = (auto || held) && PuedePagar();
        if (Time.time >= cooldown)
        {
            cooldown = ataque.cadencia > 0.0001f ? Time.time + 1f / ataque.cadencia : Time.time;
            if (boost) Pagar();
            ataque.Tick(CrearContexto(), instPermanente, boost);
        }
        ataque.Actualizar(CrearContexto(), instPermanente, boost);
    }

    void DestruirPermanente()
    {
        if (instPermanente) { Object.Destroy(instPermanente); instPermanente = null; }
    }

    AtaqueContext CrearContexto()
    {
        Vector3 originPos = origen != null ? origen.position : owner.position;
        Vector2 fwd = objetivo != null
            ? ((Vector2)(objetivo.position - originPos)).normalized
            : (Vector2)owner.up;

        return new AtaqueContext
        {
            owner = owner,
            origen = origen,
            objetivo = objetivo,
            forward = fwd,
            stats = stats
        };
    }

    bool PuedePagar()
    {
        if (stats == null || ataque == null) return true;
        if (ataque.costoCalor > 0 && stats.Recalentado) return false;
        if (ataque.costoEnergia > 0 && stats.energia < ataque.costoEnergia) return false;
        if (ataque.costoMunicion > 0 && stats.municion < ataque.costoMunicion) return false;
        return true;
    }

    void Pagar()
    {
        if (stats == null || ataque == null) return;
        if (ataque.costoEnergia > 0) stats.DrenarEnergia(ataque.costoEnergia);
        if (ataque.costoMunicion > 0) stats.ConsumirMunicion(ataque.costoMunicion);
        if (ataque.costoCalor > 0) stats.AñadirCalor(ataque.costoCalor);
    }
}