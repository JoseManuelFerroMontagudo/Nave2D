using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ① Ya NO es [RequireComponent(typeof(Stats))]
public class ShipWeapons : MonoBehaviour
{
    public AtaqueBase[] arsenal;
    public int indicePrimario = 0;
    public int indiceSecundario = 1;
    public int cañonesBase = 2;
    public float anchoCañones = 2f;

    // ② Para torretas y copias
    public bool autoDisparoPrimario = false;

    readonly List<Transform> cañones = new();
    float cdPrim, cdSec;
    bool primMantenido, secMantenido, secPresion;
    GameObject instPrim, instSec;
    AtaqueBase atkPrimCache, atkSecCache;
    Stats stats;

    public IReadOnlyList<Transform> Cañones => cañones;

    AtaqueBase Atk(int i) =>
        (arsenal == null || arsenal.Length == 0) ? null : arsenal[Mathf.Clamp(i, 0, arsenal.Length - 1)];

    void Awake()
    {
        stats = GetComponent<Stats>();   // puede ser null
        float sep = anchoCañones / Mathf.Max(1, cañonesBase);
        float centro = (cañonesBase - 1) * 0.5f;
        for (int i = 0; i < cañonesBase; i++)
        {
            var c = new GameObject($"Cañon_{i}").transform;
            c.SetParent(transform, false);
            c.localPosition = Vector3.right * (i - centro) * sep;
            cañones.Add(c);
        }
    }

    public void OnPrimaryAttack(InputValue v) => primMantenido = v.isPressed;
    public void OnSecondaryAttack(InputValue v)
    {
        if (v.isPressed) { secMantenido = true; secPresion = true; } else secMantenido = false;
    }
    public void OnSwap() => (indicePrimario, indiceSecundario) = (indiceSecundario, indicePrimario);

    void Update()
    {
        bool prim = primMantenido || autoDisparoPrimario;   // ③
        Procesar(Atk(indicePrimario), prim, false, true, ref cdPrim, ref instPrim, ref atkPrimCache);
        bool pres = secPresion; secPresion = false;
        Procesar(Atk(indiceSecundario), secMantenido, pres, false, ref cdSec, ref instSec, ref atkSecCache);
    }

    void Procesar(AtaqueBase atk, bool mantenido, bool presion, bool auto,
                  ref float cd, ref GameObject inst, ref AtaqueBase anterior)
    {
        if (atk != anterior)
        {
            if (inst) Destroy(inst);
            inst = null; anterior = atk; cd = Time.time;
            if (atk != null && atk.EsPermanente) inst = atk.Activar(this);
        }
        if (atk == null) return;

        if (atk.EsPermanente)
        {
            bool boost = mantenido && PuedePagar(atk);
            if (Time.time >= cd)
            {
                cd = Time.time + 1f / Mathf.Max(0.0001f, atk.cadencia);
                if (boost) Pagar(atk);
                atk.Tick(this, inst, boost);
            }
            atk.Actualizar(this, inst, boost);
            return;
        }

        if ((auto ? mantenido : presion) && Time.time >= cd && PuedePagar(atk))
        {
            Pagar(atk);
            cd = Time.time + 1f / Mathf.Max(0.0001f, atk.cadencia);
            atk.Ejecutar(this);
        }
    }

    // ④ Tolerante a Stats nulo
    bool PuedePagar(AtaqueBase a) =>
        stats == null ||
        (!(a.costoCalor > 0 && stats.Recalentado) &&
         (a.costoEnergia == 0 || stats.energia >= a.costoEnergia) &&
         (a.costoMunicion == 0 || stats.municion >= a.costoMunicion));

    void Pagar(AtaqueBase a)
    {
        if (stats == null) return;
        if (a.costoEnergia > 0) stats.DrenarEnergia(a.costoEnergia);
        if (a.costoMunicion > 0) stats.ConsumirMunicion(a.costoMunicion);
        if (a.costoCalor > 0) stats.AñadirCalor(a.costoCalor);
    }

    public void Disparar(GameObject prefab, float velocidad, Transform origen)
    {
        if (!prefab) return;
        var go = Instantiate(prefab, origen.position, origen.rotation);
        if (go.TryGetComponent<Projectile>(out var p)) p.Initialize(origen.up * velocidad);
    }
}