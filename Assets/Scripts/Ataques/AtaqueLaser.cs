using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Laser Configurable")]
public class AtaqueLaser : AtaqueBase
{
    [Header("Tag de impacto")]
    public string tagRayo = "Attack2";

    [Header("Geometría")]
    public float longitud = 30f;
    public string sortingLayerName = "Elementos_Juego";

    [Header("Apariencia")]
    [Tooltip("Stretch = la textura se estira a lo largo del rayo. Tile = se repite.")]
    public LineTextureMode textureMode = LineTextureMode.Tile;
    [Tooltip("Invierte el orden de los puntos del LineRenderer para que el patrón del material " +
             "avance en sentido contrario (del destino hacia el origen).")]
    public bool invertirDireccionMaterial = false;

    [Header("Ciclo")]
    public bool autoCiclico = true;
    public float pausaIdle = 0.5f;
    public float tiempoAdvertencia = 1.25f;
    public float duracionDisparo = 0.5f;

    [Header("Rotación / Barrido (grados)")]
    public float anguloInicial = 0f;
    public float barridoTotal = 0f;
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
    public int danoMinimo = 0;
    public int danoMaximo = 50;
    public bool danoEnAdvertencia = false;
    public override bool EsPermanente => true;
    public override bool UsaAudioPropio => true;

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