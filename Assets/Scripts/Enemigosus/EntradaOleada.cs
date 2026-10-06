using System;
using UnityEngine;

[Serializable]
public class EntradaOleada
{
    [Header("Composición")]
    public GameObject prefabEnemigo;
    public int cantidad = 4;

    [Header("Formación")]
    public Formacion formacion = Formacion.V;
    public float separacionLateral = 1.5f;
    public float profundidadV = 1.2f;

    [Header("Spawn")]
    public float distanciaFueraDeCamara = 14f;
    public float dispersionAngular = 20f;

    [Header("Tiempos")]
    public float retardoAntes = 1f;
    public float retardoDespues = 1.5f;
    public float delayEntreEnemigos = 0.15f;

    public enum Formacion { V, Linea, Circulo, Aleatoria }
}