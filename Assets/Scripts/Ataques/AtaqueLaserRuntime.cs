using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class LaserHitbox : MonoBehaviour, IDamageDealer
{
    public int Daño { get; set; }
}

public class AtaqueLaserRuntime : MonoBehaviour, IPermanenteHandler
{
    public AtaqueLaser Config;

    [Tooltip("Nombre del grupo del mixer para el audio del láser.")]
    public string mixerGroupSFX = "SFX";

    enum Estado { Idle, Advertencia, Disparo }
    Estado estado = Estado.Idle;

    bool boostAnterior;
    float timer;
    float rotacion;

    float enfriamiento;
    float ultimoInicioCarga = -999f;

    int danioAcumulado;
    float grosorAcumulado;

    AudioSource audioDisparo;
    AudioSource audioCarga;

    class Rayo
    {
        public LineRenderer lr;
        public EdgeCollider2D col;
        public LaserHitbox hb;
    }
    readonly List<Rayo> rayos = new();

    public int DanioActual { get; private set; }
    public float GrosorActual { get; private set; }

    public void SetBoost(bool boost) { }

    void Awake()
    {
        var grupo = ResolverGrupo(mixerGroupSFX);

        audioDisparo = gameObject.AddComponent<AudioSource>();
        audioDisparo.loop = true;
        audioDisparo.playOnAwake = false;
        audioDisparo.spatialBlend = 0f;
        audioDisparo.outputAudioMixerGroup = grupo;

        audioCarga = gameObject.AddComponent<AudioSource>();
        audioCarga.loop = true;
        audioCarga.playOnAwake = false;
        audioCarga.spatialBlend = 0f;
        audioCarga.outputAudioMixerGroup = grupo;
    }

    void OnDestroy()
    {
        if (audioDisparo != null && audioDisparo.isPlaying) audioDisparo.Stop();
        if (audioCarga != null && audioCarga.isPlaying) audioCarga.Stop();
    }

    public void Procesar(AtaqueContext ctx, bool boost)
    {
        if (Config == null) return;
        float dt = Time.deltaTime;

        if (enfriamiento > 0f) enfriamiento -= dt;

        ProcesarTransicion(boost, dt);
        ProcesarEstado(ctx, dt, boost);
        boostAnterior = boost;
    }

    void ProcesarTransicion(bool boost, float dt)
    {
        bool flancoSubida = boost && !boostAnterior;
        bool flancoBajada = !boost && boostAnterior;

        switch (estado)
        {
            case Estado.Idle:
                bool puedeEntrar = enfriamiento <= 0f && CadenciaLista();
                if (puedeEntrar && (flancoSubida || (Config.autoCiclico && boost)))
                    Entrar(Estado.Advertencia);
                break;

            case Estado.Advertencia:
                if (flancoBajada)
                {
                    if (DanioActual > 0 || GrosorActual > 0.0001f) CongelarYDisparar();
                    else Entrar(Estado.Idle);
                }
                break;

            case Estado.Disparo:
                break;
        }
    }

    void CongelarYDisparar()
    {
        danioAcumulado = DanioActual;
        grosorAcumulado = GrosorActual;
        Entrar(Estado.Disparo);
    }

    bool CadenciaLista()
        => Time.time >= ultimoInicioCarga + (Config.cadencia > 0.0001f ? 1f / Config.cadencia : 0f);

    void ProcesarEstado(AtaqueContext ctx, float dt, bool boost)
    {
        switch (estado)
        {
            case Estado.Idle:
                DanioActual = 0;
                GrosorActual = 0f;
                OcultarRayos();
                ActualizarAudio(cargando: false, disparando: false);
                break;

            case Estado.Advertencia:
                timer += dt;
                float t01 = Mathf.Clamp01(timer / Mathf.Max(0.0001f, Config.tiempoAdvertencia));

                rotacion = Config.anguloInicial
                         + Config.barridoTotal * t01
                         + Config.rotacionAdvertencia * timer;

                DanioActual = Config.danoEnAdvertencia ? Config.DanioEnAdvertencia(t01) : 0;
                GrosorActual = Mathf.Lerp(0f, Config.grosorDisparo, t01);
                DibujarRayos(ctx, advertencia: true);
                ActualizarAudio(cargando: Config.sonidoCarga != null, disparando: false);

                if (timer >= Config.tiempoAdvertencia)
                {
                    timer = Config.tiempoAdvertencia;
                    DanioActual = Config.danoEnAdvertencia ? Config.danoMaximo : 0;
                    GrosorActual = Config.grosorDisparo;
                    rotacion = Config.anguloInicial
                             + Config.barridoTotal
                             + Config.rotacionAdvertencia * Config.tiempoAdvertencia;

                    if (Config.autoCiclico) CongelarYDisparar();
                }
                break;

            case Estado.Disparo:
                timer += dt;
                rotacion += Config.rotacionDisparo * dt;
                DanioActual = danioAcumulado;
                GrosorActual = grosorAcumulado;
                DibujarRayos(ctx, advertencia: false);
                ActualizarAudio(cargando: false, disparando: Config.sonidoDisparo != null);
                if (timer >= Config.duracionDisparo) Entrar(Estado.Idle);
                break;
        }
    }

    void ActualizarAudio(bool cargando, bool disparando)
    {
        if (Config == null) return;

        if (cargando)
        {
            if (audioCarga.clip != Config.sonidoCarga) audioCarga.clip = Config.sonidoCarga;
            audioCarga.volume = Config.volumenCarga;
            if (!audioCarga.isPlaying) audioCarga.Play();
        }
        else if (audioCarga.isPlaying) audioCarga.Stop();

        if (disparando)
        {
            if (audioDisparo.clip != Config.sonidoDisparo) audioDisparo.clip = Config.sonidoDisparo;
            audioDisparo.volume = Config.volumenDisparo;
            if (!audioDisparo.isPlaying) audioDisparo.Play();
        }
        else if (audioDisparo.isPlaying) audioDisparo.Stop();
    }

    void Entrar(Estado nuevo)
    {
        estado = nuevo;
        timer = 0f;

        switch (nuevo)
        {
            case Estado.Advertencia:
                rotacion = 0f;
                danioAcumulado = 0;
                grosorAcumulado = 0f;
                DanioActual = 0;
                GrosorActual = 0f;
                ultimoInicioCarga = Time.time;
                break;

            case Estado.Disparo:
                if (danioAcumulado <= 0) danioAcumulado = Config != null ? Config.danoMaximo : 0;
                if (grosorAcumulado <= 0f) grosorAcumulado = Config != null ? Config.grosorDisparo : 0f;
                DanioActual = danioAcumulado;
                GrosorActual = grosorAcumulado;
                if (audioCarga != null && audioCarga.isPlaying) audioCarga.Stop();
                break;

            case Estado.Idle:
                rotacion = 0f;
                DanioActual = 0;
                GrosorActual = 0f;
                if (Config != null) enfriamiento = Mathf.Max(enfriamiento, Config.pausaIdle);
                OcultarRayos();
                if (audioDisparo != null && audioDisparo.isPlaying) audioDisparo.Stop();
                if (audioCarga != null && audioCarga.isPlaying) audioCarga.Stop();
                break;
        }
    }

    void OcultarRayos()
    {
        foreach (var r in rayos)
        {
            if (r.lr) { r.lr.positionCount = 0; r.lr.enabled = false; }
            if (r.col) r.col.enabled = false;
        }
    }

    void DibujarRayos(AtaqueContext ctx, bool advertencia)
    {
        if (GrosorActual <= 0.0001f) { OcultarRayos(); return; }

        int total = Mathf.Max(1, Config.cantidadCañones);
        AsegurarRayos(total);

        Color color = advertencia ? Config.colorAdvertencia : Config.colorDisparo;
        Material mat = advertencia ? Config.materialAdvertencia : Config.materialDisparo;
        float edgeRadius = (advertencia && !Config.danoEnAdvertencia) ? 0f : GrosorActual * 0.5f;
        bool damageEnabled = !advertencia || Config.danoEnAdvertencia;

        for (int i = 0; i < total; i++)
        {
            CalcularOrigenYDir(ctx, i, total, out Vector3 origen, out Vector2 dirBase);

            float anguloBase = Mathf.Atan2(dirBase.y, dirBase.x) * Mathf.Rad2Deg + rotacion;
            float ang = anguloBase * Mathf.Deg2Rad;
            Vector3 dirRayo = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);

            var puntosMundo = GenerarPuntos(origen, dirRayo);
            var r = rayos[i];

            r.lr.enabled = true;
            if (mat != null) r.lr.material = mat;
            r.lr.startWidth = r.lr.endWidth = GrosorActual;
            r.lr.startColor = r.lr.endColor = color;
            r.lr.textureMode = Config.textureMode;

            Transform rt = r.lr.transform;
            var local = new Vector3[puntosMundo.Count];
            for (int p = 0; p < puntosMundo.Count; p++)
                local[p] = rt.InverseTransformPoint(puntosMundo[p]);

            if (Config.invertirDireccionMaterial)
            {
                var invertido = new Vector3[local.Length];
                for (int p = 0; p < local.Length; p++)
                    invertido[p] = local[local.Length - 1 - p];
                r.lr.positionCount = invertido.Length;
                r.lr.SetPositions(invertido);
            }
            else
            {
                r.lr.positionCount = local.Length;
                r.lr.SetPositions(local);
            }

            r.col.points = ToVec2Array(local);
            r.col.edgeRadius = edgeRadius;
            r.col.enabled = damageEnabled;
            r.hb.Daño = DanioActual;
        }

        for (int i = total; i < rayos.Count; i++)
        {
            rayos[i].lr.enabled = false;
            rayos[i].col.enabled = false;
        }
    }

    void CalcularOrigenYDir(AtaqueContext ctx, int i, int total, out Vector3 pos, out Vector2 dir)
    {
        if (total < 1) total = 1;
        Vector3 basePos = ctx.origen != null ? ctx.origen.position : ctx.owner.position;
        Vector2 fwd = ctx.forward.sqrMagnitude > 0.0001f ? ctx.forward.normalized : Vector2.up;

        if (Config.modoSeparacion == ModoSeparacion.Distancia)
        {
            Vector2 right = new Vector2(fwd.y, -fwd.x);
            float offset = total > 1 ? (i - (total - 1) * 0.5f) * Config.separacion : 0f;
            pos = basePos + (Vector3)(right * offset);
            dir = fwd;
        }
        else
        {
            pos = basePos;
            float angOffset = total > 1 ? (i - (total - 1) * 0.5f) * Config.separacion : 0f;
            float baseAng = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg + angOffset;
            float rad = baseAng * Mathf.Deg2Rad;
            dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    List<Vector3> GenerarPuntos(Vector3 origen, Vector3 dir)
    {
        var pts = new List<Vector3> { origen };
        if (!Config.zigzag)
        {
            pts.Add(origen + dir * Config.longitud);
            return pts;
        }
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
        float paso = Config.longitud / Mathf.Max(1, Config.segmentosZigZag);
        for (int i = 1; i < Config.segmentosZigZag; i++)
        {
            float signo = (i % 2 == 0) ? 1f : -1f;
            pts.Add(origen + dir * (paso * i) + perp * (Config.amplitudZigZag * signo));
        }
        pts.Add(origen + dir * Config.longitud);
        return pts;
    }

    void AsegurarRayos(int n)
    {
        while (rayos.Count < n)
        {
            var go = new GameObject("Rayo_" + rayos.Count);
            go.transform.SetParent(transform, false);
            go.tag = Config.tagRayo;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sortingLayerName = Config.sortingLayerName;
            lr.textureMode = Config.textureMode;
            lr.enabled = false;

            var col = go.AddComponent<EdgeCollider2D>();
            col.isTrigger = true;
            col.enabled = false;

            var hb = go.AddComponent<LaserHitbox>();
            hb.Daño = 0;

            rayos.Add(new Rayo { lr = lr, col = col, hb = hb });
        }
    }

    static Vector2[] ToVec2Array(Vector3[] v)
    {
        var r = new Vector2[v.Length];
        for (int i = 0; i < v.Length; i++) r[i] = v[i];
        return r;
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