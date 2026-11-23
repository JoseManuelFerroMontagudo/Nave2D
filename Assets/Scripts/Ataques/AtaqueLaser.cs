using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Laser Configurable")]
public class AtaqueLaser : AtaqueBase
{
    public enum ModoLaser { AutoCiclico, CargaSoltar, Instantaneo }

    [Header("Modo")]
    public ModoLaser modo = ModoLaser.AutoCiclico;

    [Header("Tag de impacto")]
    [Tooltip("Debe ser un tag que el receptor procese con OnTriggerStay2D (típicamente 'Attack2'). " +
             "NO usar 'Bullet' si el receptor destruye el source al OnTriggerEnter.")]
    public string tagRayo = "Attack2";

    [Header("Geometría")]
    public int cantidadRayos = 2;
    public float anguloAbanico = 18f;
    public float longitud = 30f;
    public string sortingLayerName = "Elementos_Juego";

    [Header("Ciclo")]
    public float pausaIdle = 2f;
    public float tiempoAdvertencia = 1.25f;
    public float duracionDisparo = 0.5f;

    [Header("Rotación (grados/seg)")]
    public float rotacionAdvertencia = 0f;
    public float rotacionDisparo = 0f;

    [Header("Visual")]
    public Color colorAdvertencia = new Color(1f, 0.9f, 0.1f, 0.45f);
    public float grosorAdvertencia = 0.14f;
    public Color colorDisparo = new Color(1f, 0.1f, 0.1f, 1f);
    public float grosorDisparo = 1.0f;
    public Material materialAdvertencia;
    public Material materialDisparo;

    [Header("Zig-Zag")]
    public bool zigzag = false;
    public int segmentosZigZag = 10;
    public float amplitudZigZag = 1.1f;

    [Header("Daño (por segundo)")]
    [Tooltip("Daño al inicio de la advertencia. Rampa hasta danoMaximo al terminar la advertencia.")]
    public int danoMinimo = 10;
    [Tooltip("Daño máximo. Es el que se usa durante el disparo.")]
    public int danoMaximo = 50;
    [Tooltip("Si está activo, la advertencia ya aplica el daño rampante. " +
             "Desactívalo si quieres que la advertencia sea solo telegráfica.")]
    public bool danoEnAdvertencia = true;

    public override bool EsPermanente => true;

    /// <summary>t01 = progreso normalizado [0..1] de la fase de advertencia.</summary>
    public int DanioEnAdvertencia(float t01)
        => Mathf.RoundToInt(Mathf.Lerp(danoMinimo, danoMaximo, Mathf.Clamp01(t01)));

    public override GameObject Activar(AtaqueContext ctx)
    {
        var go = new GameObject("LaserRuntime_" + (string.IsNullOrEmpty(nombreVisible) ? "Laser" : nombreVisible));
        go.transform.SetParent(ctx.owner, false);
        var rt = go.AddComponent<AtaqueLaserRuntime>();
        rt.Config = this;
        return go;
    }

    public override void Actualizar(AtaqueContext ctx, GameObject inst, bool boost)
    {
        if (inst != null && inst.TryGetComponent<AtaqueLaserRuntime>(out var rt))
            rt.Procesar(ctx, boost);
    }
}