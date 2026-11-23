using System.Collections.Generic;
using UnityEngine;

public class LaserHitbox : MonoBehaviour, IDamageDealer
{
    public int Daño { get; set; }
}

public class AtaqueLaserRuntime : MonoBehaviour
{
    public AtaqueLaser Config;

    enum Estado { Idle, Advertencia, Disparo }
    Estado estado = Estado.Idle;

    bool boostAnterior;
    float timer, rotacion;

    class Rayo
    {
        public LineRenderer lr;
        public EdgeCollider2D col;
        public LaserHitbox hb;
    }
    readonly List<Rayo> rayos = new();

    public int DanioActual { get; private set; }
    public float GrosorActual { get; private set; }

    public void Procesar(AtaqueContext ctx, bool boost)
    {
        if (Config == null) return;
        float dt = Time.deltaTime;

        ProcesarTransicion(boost, dt);
        ProcesarEstado(ctx, dt);
        boostAnterior = boost;
    }

    void ProcesarTransicion(bool boost, float dt)
    {
        switch (Config.modo)
        {
            case AtaqueLaser.ModoLaser.AutoCiclico:
                if (!boost && estado != Estado.Idle) VolverIdle();
                else if (boost && estado == Estado.Idle)
                {
                    timer += dt;
                    if (timer >= Config.pausaIdle) Entrar(Estado.Advertencia);
                }
                break;

            case AtaqueLaser.ModoLaser.CargaSoltar:
                if (!boostAnterior && boost && estado == Estado.Idle) Entrar(Estado.Advertencia);
                else if (boostAnterior && !boost && estado == Estado.Advertencia) Entrar(Estado.Disparo);
                break;

            case AtaqueLaser.ModoLaser.Instantaneo:
                if (!boostAnterior && boost && estado == Estado.Idle) Entrar(Estado.Disparo);
                break;
        }
    }

    void ProcesarEstado(AtaqueContext ctx, float dt)
    {
        switch (estado)
        {
            case Estado.Idle:
                break;

            case Estado.Advertencia:
                timer += dt;
                rotacion += Config.rotacionAdvertencia * dt;
                float t01 = Mathf.Clamp01(timer / Mathf.Max(0.0001f, Config.tiempoAdvertencia));
                DanioActual = Config.DanioEnAdvertencia(t01);
                GrosorActual = Mathf.Lerp(Config.grosorAdvertencia, Config.grosorDisparo, t01);
                DibujarRayos(ctx, advertencia: true);
                if (Config.modo != AtaqueLaser.ModoLaser.CargaSoltar && timer >= Config.tiempoAdvertencia)
                    Entrar(Estado.Disparo);
                break;

            case Estado.Disparo:
                timer += dt;
                rotacion += Config.rotacionDisparo * dt;
                //GrosorActual = Config.grosorDisparo;
                DibujarRayos(ctx, advertencia: false);
                if (timer >= Config.duracionDisparo) VolverIdle();
                break;
        }
    }

    void Entrar(Estado nuevo)
    {
        estado = nuevo;
        timer = 0f;
        if (Config == null) return;
        if (nuevo == Estado.Advertencia)
        {
            DanioActual = Config.danoMinimo;
            GrosorActual = Config.grosorAdvertencia;
        }
        else if (nuevo == Estado.Disparo)
        {
            //DanioActual = Config.danoMaximo;
            //GrosorActual = Config.grosorDisparo;
        }
    }

    void VolverIdle()
    {
        estado = Estado.Idle;
        timer = 0f;
        if (Config != null)
        {
            DanioActual = Config.danoMinimo;
            GrosorActual = Config.grosorAdvertencia;
        }
        foreach (var r in rayos) { r.lr.enabled = false; r.col.enabled = false; }
    }

    void DibujarRayos(AtaqueContext ctx, bool advertencia)
    {
        Vector3 origen = ctx.origen != null ? ctx.origen.position : ctx.owner.position;
        Vector3 dir = ctx.objetivo != null
            ? (ctx.objetivo.position - origen).normalized
            : (Vector3)ctx.forward;
        if (dir == Vector3.zero) dir = Vector3.up;

        float anguloBase = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotacion;
        int total = Mathf.Max(1, Config.cantidadRayos);

        AsegurarRayos(total);
        float grosor = GrosorActual;
        Color color = advertencia ? Config.colorAdvertencia : Config.colorDisparo;
        Material mat = advertencia ? Config.materialAdvertencia : Config.materialDisparo;
        float edgeRadius = (advertencia && !Config.danoEnAdvertencia) ? 0f : GrosorActual * 0.5f;
        bool damageEnabled = !advertencia || Config.danoEnAdvertencia;

        for (int i = 0; i < total; i++)
        {
            float offset = (total > 1)
                ? Mathf.Lerp(-Config.anguloAbanico * 0.5f, Config.anguloAbanico * 0.5f, i / (total - 1f))
                : 0f;
            float ang = (anguloBase + offset) * Mathf.Deg2Rad;
            Vector3 dirRayo = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);

            var puntosMundo = GenerarPuntos(origen, dirRayo);
            var r = rayos[i];

            r.lr.enabled = true;
            if (mat != null) r.lr.material = mat;
            r.lr.startWidth = r.lr.endWidth = grosor;
            r.lr.startColor = r.lr.endColor = color;

            Transform rt = r.lr.transform;
            var local = new Vector3[puntosMundo.Count];
            for (int p = 0; p < puntosMundo.Count; p++)
                local[p] = rt.InverseTransformPoint(puntosMundo[p]);

            r.lr.positionCount = local.Length;
            r.lr.SetPositions(local);

            r.col.points = ToVec2Array(local);
            r.col.edgeRadius = edgeRadius;
            r.col.enabled = damageEnabled;
            r.hb.Daño = DanioActual;
            Debug.Log($"Daño actual del rayo {i}: {r.hb.Daño}");
        }

        for (int i = total; i < rayos.Count; i++)
        {
            rayos[i].lr.enabled = false;
            rayos[i].col.enabled = false;
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
        float paso = Config.longitud / Config.segmentosZigZag;
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
            lr.textureMode = LineTextureMode.Tile;
            lr.enabled = false;

            var col = go.AddComponent<EdgeCollider2D>();
            col.isTrigger = true;
            col.enabled = false;

            var hb = go.AddComponent<LaserHitbox>();
            hb.Daño = Config.danoMinimo;

            rayos.Add(new Rayo { lr = lr, col = col, hb = hb });
        }
    }

    static Vector2[] ToVec2Array(Vector3[] v)
    {
        var r = new Vector2[v.Length];
        for (int i = 0; i < v.Length; i++) r[i] = v[i];
        return r;
    }
}