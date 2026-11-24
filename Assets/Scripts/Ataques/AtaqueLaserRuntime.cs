using System.Collections.Generic;
using UnityEngine;

public class LaserHitbox : MonoBehaviour, IDamageDealer
{
    public int Daño { get; set; }
}

public class AtaqueLaserRuntime : MonoBehaviour, IPermanenteHandler
{
    public AtaqueLaser Config;

    enum Estado { Idle, Advertencia, Disparo }
    Estado estado = Estado.Idle;

    bool boostAnterior;
    float timer;
    float rotacion;

    float enfriamiento;
    float ultimoInicioCarga = -999f;

    int danioAcumulado;
    float grosorAcumulado;

    class Rayo
    {
        public LineRenderer lr;
        public EdgeCollider2D col;
        public LaserHitbox hb;
    }
    readonly List<Rayo> rayos = new();

    public int DanioActual { get; private set; }
    public float GrosorActual { get; private set; }

    public void SetBoost(bool boost) { /* boost llega por Procesar */ }

    public void Procesar(AtaqueContext ctx, bool boost)
    {
        if (Config == null) return;
        float dt = Time.deltaTime;

        if (enfriamiento > 0f) enfriamiento -= dt;

        ProcesarTransicion(boost, dt);
        ProcesarEstado(ctx, dt, boost);   // ← pasamos boost para auto-disparo
        boostAnterior = boost;
    }

    // ------------------ TRANSICIONES ------------------
    void ProcesarTransicion(bool boost, float dt)
    {
        bool flancoSubida = boost && !boostAnterior;
        bool flancoBajada = !boost && boostAnterior;

        switch (estado)
        {
            case Estado.Idle:
                // Entrada por flanco (manual) o por nivel si es cíclico y ya pasó el enfriamiento.
                bool puedeEntrar = enfriamiento <= 0f && CadenciaLista();
                if (puedeEntrar && (flancoSubida || (Config.autoCiclico && boost)))
                    Entrar(Estado.Advertencia);
                break;

            case Estado.Advertencia:
                // Pérdida de boost (soltar o quedarse sin recursos) → disparar con lo acumulado.
                if (flancoBajada)
                {
                    if (DanioActual > 0 || GrosorActual > 0.0001f)
                        CongelarYDisparar();
                    else
                        Entrar(Estado.Idle);   // no había carga aún: cancelar sin gastar
                }
                break;

            case Estado.Disparo:
                // En cíclico, si sigue pulsado y ya pasó el enfriamiento, re-carga.
                // (El enfriamiento se aplica al volver a Idle, no aquí.)
                break;
        }
    }

    void CongelarYDisparar()
    {
        // Usa lo acumulado; si es 0 (corte instantáneo), usa el mínimo, no el máximo.
        danioAcumulado = DanioActual;
        grosorAcumulado = GrosorActual;
        Entrar(Estado.Disparo);
    }

    bool CadenciaLista()
        => Time.time >= ultimoInicioCarga + (Config.cadencia > 0.0001f ? 1f / Config.cadencia : 0f);

    // ------------------ ESTADOS ------------------
    void ProcesarEstado(AtaqueContext ctx, float dt, bool boost)
    {
        switch (estado)
        {
            case Estado.Idle:
                DanioActual = 0;
                GrosorActual = 0f;
                OcultarRayos();
                break;

            case Estado.Advertencia:
                timer += dt;
                float t01 = Mathf.Clamp01(timer / Mathf.Max(0.0001f, Config.tiempoAdvertencia));

                rotacion = Config.anguloInicial
                         + Config.barridoTotal * t01
                         + Config.rotacionAdvertencia * timer;

                DanioActual = Config.danoEnAdvertencia
                    ? Config.DanioEnAdvertencia(t01)
                    : 0;
                GrosorActual = Mathf.Lerp(0f, Config.grosorDisparo, t01);
                DibujarRayos(ctx, advertencia: true);

                if (timer >= Config.tiempoAdvertencia)
                {
                    timer = Config.tiempoAdvertencia;
                    DanioActual = Config.danoEnAdvertencia ? Config.danoMaximo : 0;
                    GrosorActual = Config.grosorDisparo;
                    rotacion = Config.anguloInicial
                             + Config.barridoTotal
                             + Config.rotacionAdvertencia * Config.tiempoAdvertencia;

                    // ⭐ Auto-disparo en cíclico: al alcanzar el máximo, dispara solo
                    if (Config.autoCiclico)
                        CongelarYDisparar();
                    // En manual, se queda cargado hasta que el jugador suelte.
                }
                break;

            case Estado.Disparo:
                timer += dt;
                rotacion += Config.rotacionDisparo * dt;
                DanioActual = danioAcumulado;
                GrosorActual = grosorAcumulado;
                DibujarRayos(ctx, advertencia: false);
                if (timer >= Config.duracionDisparo) Entrar(Estado.Idle);
                break;
        }
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
                break;

            case Estado.Idle:
                rotacion = 0f;
                DanioActual = 0;
                GrosorActual = 0f;
                if (Config != null) enfriamiento = Mathf.Max(enfriamiento, Config.pausaIdle);
                OcultarRayos();
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

    // ------------------ DIBUJO ------------------
    void DibujarRayos(AtaqueContext ctx, bool advertencia)
    {
        if (GrosorActual <= 0.0001f)
        {
            OcultarRayos();
            return;
        }

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
            r.lr.textureMode = Config.textureMode;    // ⭐ por si se cambia en runtime

            // Puntos en local del LineRenderer
            Transform rt = r.lr.transform;
            var local = new Vector3[puntosMundo.Count];
            for (int p = 0; p < puntosMundo.Count; p++)
                local[p] = rt.InverseTransformPoint(puntosMundo[p]);

            // Orden para el LineRenderer (invertido si se pide)
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

            // El collider no usa UV: da igual el orden, pero usamos el original
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

    void CalcularOrigenYDir(AtaqueContext ctx, int i, int total,
                            out Vector3 pos, out Vector2 dir)
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
            lr.textureMode = Config.textureMode;      // ⭐ aplica el modo configurado
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
}