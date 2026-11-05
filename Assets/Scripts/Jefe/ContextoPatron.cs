using UnityEngine;

public class ContextoPatron
{
    public ControladorJefe jefe;
    public Transform puntoDisparo;
    public Transform jugador;

    public Vector2 DireccionDesdeGrados(float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    public float AnguloAlJugador()
    {
        if (jugador == null) return 0f;
        Vector2 hacia = (jugador.position - jefe.transform.position).normalized;
        return Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;
    }

    public void Disparar(Vector2 direccion, OpcionesProyectil opciones)
        => jefe.InstanciarProyectil(puntoDisparo, direccion, opciones);

    public void DispararCirculo(int cantidad, float offsetGrados, OpcionesProyectil opciones)
    {
        float paso = 360f / cantidad;
        for (int i = 0; i < cantidad; i++)
            Disparar(DireccionDesdeGrados(paso * i + offsetGrados), opciones);
    }
}