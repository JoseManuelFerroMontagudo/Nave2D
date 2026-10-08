using UnityEngine;

public class AnimacionBorde : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float velocidadPulso = 2f;
    private float grosorMin = 0.2f;
    private float grosorMax = 0.5f;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (lineRenderer == null) return;

        // Calcula un pulso suave usando Sine
        float t = (Mathf.Sin(Time.time * velocidadPulso) + 1f) / 2f;
        float grosorActual = Mathf.Lerp(grosorMin, grosorMax, t);

        lineRenderer.startWidth = grosorActual;
        lineRenderer.endWidth = grosorActual;
    }
}