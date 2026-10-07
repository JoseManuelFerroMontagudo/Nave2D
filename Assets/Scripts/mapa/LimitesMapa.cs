using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LimitesMapa : MonoBehaviour
{
    public static LimitesMapa Instancia { get; private set; }

    [Header("Configuración del Borde")]
    [SerializeField] private float radioMapa = 300f;
    [SerializeField] private Vector3 centroMapa = Vector3.zero;
    [SerializeField] private int segmentos = 128; // Mayor número = círculo más suave

    private LineRenderer lineRenderer;

    public float RadioMapa => radioMapa;
    public Vector3 CentroMapa => centroMapa;

    private void Awake()
    {
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);

        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        DibujarBordeVisual();
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void DibujarBordeVisual()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = segmentos;
        lineRenderer.useWorldSpace = true;

        float pasoAngulo = 360f / segmentos;

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * pasoAngulo * Mathf.Deg2Rad;
            // Generamos las posiciones en el plano XY
            Vector3 punto = centroMapa + new Vector3(Mathf.Cos(angulo) * radioMapa, Mathf.Sin(angulo) * radioMapa, 0f);
            lineRenderer.SetPosition(i, punto);
        }
    }

    // Dibujar Gizmo para el editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centroMapa, radioMapa);
    }
}