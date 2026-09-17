using UnityEngine;

[System.Serializable]
public class EntradaPatron
{
    [Tooltip("Patrón (ScriptableObject) a ejecutar")]
    public PatronBase patron;

    [Tooltip("Segundos a esperar ANTES de iniciar este patrón")]
    public float retardoAntes = 0f;

    [Tooltip("Segundos a esperar DESPUÉS de terminar este patrón")]
    public float retardoDespues = 2f;
}