using System.Collections.Generic;
using UnityEngine;

public class SeparacionEnemigos : MonoBehaviour
{
    [Header("Separación")]
    public float radioSeparacion = 1.2f;   // distancia mínima entre enemigos
    public float fuerzaSeparacion = 3f;    // qué tan fuerte se empujan

    [Header("Detección")]
    public string tagEnemigos = "Enemy";   // tag que usan los enemigos
    public float intervaloActualizacion = 0.2f; // cada cuánto recalcula (rendimiento)

    private float cronometro;
    private static readonly List<SeparacionEnemigos> todos = new List<SeparacionEnemigos>();

    void OnEnable() { todos.Add(this); }
    void OnDisable() { todos.Remove(this); }

    void Update()
    {
        cronometro += Time.deltaTime;
        if (cronometro < intervaloActualizacion) return;
        cronometro = 0f;

        Vector2 empuje = Vector2.zero;
        int vecinos = 0;

        foreach (var otro in todos)
        {
            if (otro == this) continue;

            Vector2 diff = (Vector2)(transform.position - otro.transform.position);
            float dist = diff.magnitude;

            if (dist < radioSeparacion && dist > 0.0001f)
            {
                // Empuje inversamente proporcional a la distancia
                empuje += diff.normalized * (radioSeparacion - dist) / radioSeparacion;
                vecinos++;
            }
        }

        if (vecinos > 0)
        {
            empuje /= vecinos;
            transform.position += (Vector3)(empuje * fuerzaSeparacion * intervaloActualizacion);
        }
    }
}