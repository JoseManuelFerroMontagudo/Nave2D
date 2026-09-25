using UnityEngine;

public class RotacionPlaneta : MonoBehaviour
{
    public float velocidadRotacion = 20f;

    void Update()
    {
        // Rota de manera continua y ultra fluida matemáticamente
        transform.Rotate(Vector3.forward * velocidadRotacion * Time.deltaTime);
    }
}
