using UnityEngine;

public class PlanetaEspacial : MonoBehaviour
{
    public float velocidadRotacion = 20f;
    private Transform camTransform;
    private float anguloActual = 0f;

    void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camTransform != null)
        {
            // 1. Acumulamos la rotación matemática de forma fluida
            anguloActual += velocidadRotacion * Time.deltaTime;

            // 2. Primero hacemos que mire a la cámara (Billboard)
            // y luego le multiplicamos la rotación en el eje Z (Forward)
            transform.rotation = camTransform.rotation * Quaternion.Euler(0, 0, anguloActual);
        }
    }
}
