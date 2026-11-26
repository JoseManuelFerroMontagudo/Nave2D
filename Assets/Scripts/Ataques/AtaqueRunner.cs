using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AtaqueRunner
{
    public AtaqueBase ataque;
    public Transform owner;
    public Transform origen;
    public Transform objetivo;
    public Stats stats;
    public bool auto;

    public string mixerGroupSFX = "SFX";

    readonly MonoBehaviour host;

    bool held;
    float cooldown;
    AtaqueBase ataqueAnterior;
    GameObject instPermanente;

    bool enRafaga;
    Coroutine rafagaActual;

    AudioSource holdSource;

    const int SHOT_POOL_SIZE = 8;
    readonly AudioSource[] shotPool = new AudioSource[SHOT_POOL_SIZE];
    int shotPoolIdx;

    AudioMixerGroup grupoSFX;

    public AtaqueRunner(MonoBehaviour host)
    {
        this.host = host;

        grupoSFX = ResolverGrupo(mixerGroupSFX);

        for (int i = 0; i < SHOT_POOL_SIZE; i++)
        {
            var s = host.gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.outputAudioMixerGroup = grupoSFX;
            shotPool[i] = s;
        }

        holdSource = host.gameObject.AddComponent<AudioSource>();
        holdSource.loop = true;
        holdSource.playOnAwake = false;
        holdSource.spatialBlend = 0f;
        holdSource.outputAudioMixerGroup = grupoSFX;
    }

    public AtaqueBase AtaqueActual => ataque;
    public void SetHeld(bool v) => held = v;

    public void SetAtaque(AtaqueBase nuevo)
    {
        if (ataque == nuevo) return;
        DetenerRafaga();
        DetenerSonidoHold();
        DestruirPermanente();
        ataque = nuevo;
        ataqueAnterior = null;
        cooldown = 0f;
    }

    public bool TryFire(bool ignorarCooldown = false)
    {
        if (ataque == null || ataque.EsPermanente) return false;

        if (!ignorarCooldown && enRafaga) return false;
        if (!ignorarCooldown && Time.time < cooldown) return false;
        if (!PuedePagar()) return false;

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

    void ActivarPermanente()
    {
        DestruirPermanente();
        DetenerSonidoHold();
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

        ActualizarSonidoHold(boost);
    }

    void ActualizarSonidoHold(bool activo)
    {
        if (ataque == null || ataque.UsaAudioPropio || ataque.sonidoHold == null)
        {
            DetenerSonidoHold();
            return;
        }

        if (activo)
        {
            if (holdSource.clip != ataque.sonidoHold)
                holdSource.clip = ataque.sonidoHold;
            holdSource.volume = ataque.volumenHold;
            if (!holdSource.isPlaying) holdSource.Play();
        }
        else DetenerSonidoHold();
    }

    void DetenerSonidoHold()
    {
        if (holdSource != null && holdSource.isPlaying) holdSource.Stop();
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

        var ctx = new AtaqueContext
        {
            owner = owner,
            origen = origen,
            objetivo = objetivo,
            forward = fwd,
            stats = stats
        };

        // El sonido de disparo suena una vez por cada bala instanciada vía ctx.Disparar
        if (ataque != null && ataque.sonidoDisparo != null)
            ctx.sonidoDisparo = ReproducirDisparo;

        return ctx;
    }

    void ReproducirDisparo()
    {
        if (ataque == null || ataque.sonidoDisparo == null) return;

        var s = shotPool[shotPoolIdx];
        shotPoolIdx = (shotPoolIdx + 1) % SHOT_POOL_SIZE;

        s.Stop();
        s.clip = ataque.sonidoDisparo;
        s.volume = ataque.volumenDisparo;
        s.Play();
    }

    bool PuedePagar()
    {
        if (stats == null || ataque == null) return true;
        if (ataque.costoCalor > 0 && stats.Recalentado) return false;
        if (ataque.costoEnergia > 0 && stats.energia < ataque.costoEnergia) return false;
        return true;
    }

    void Pagar()
    {
        if (stats == null || ataque == null) return;
        if (ataque.costoEnergia > 0) stats.DrenarEnergia(ataque.costoEnergia);
        if (ataque.costoCalor > 0) stats.AñadirCalor(ataque.costoCalor);
    }

    static AudioMixerGroup ResolverGrupo(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        var assets = Resources.FindObjectsOfTypeAll<AudioMixer>();
        foreach (var m in assets)
        {
            var grupos = m.FindMatchingGroups(nombre);
            if (grupos != null && grupos.Length > 0) return grupos[0];
        }
        return null;
    }
}