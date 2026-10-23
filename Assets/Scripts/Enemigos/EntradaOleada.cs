using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GrupoEnemigos
{
    public GameObject prefab;
    public int cantidad = 1;
}

[Serializable]
public class EntradaOleada
{
    public List<GrupoEnemigos> grupos = new List<GrupoEnemigos>();
    public float retardoAntes = 1f;
    public float retardoDespues = 1.5f;
    public float delayEntreEnemigos = 0.15f;
}