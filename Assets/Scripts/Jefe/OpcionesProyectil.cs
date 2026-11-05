using UnityEngine;

public enum ModoMovimiento { Recto, Ondulado, Perseguidor, Desacelerar }

[System.Serializable]
public class OpcionesProyectil
{
    [Header("Movimiento")]
    public ModoMovimiento modo = ModoMovimiento.Recto;
    public float velocidad = 9f;
    public float tiempoVuelo = 2.5f;
    public float vidaUtil = 14f;
    public Color color = Color.white;

    [Header("Ondulado")]
    public float amplitudOnda = 2f;
    public float frecuenciaOnda = 8f;

    [Header("Perseguidor")]
    public float fuerzaPersecucion = 1.5f;

    [Header("Retorno al origen")]
    public bool regresaAlOrigen = true;
    [Range(0.1f, 1.5f)] public float multiplicadorVelocidadRetorno = 0.7f;
    public float tiempoFrenado = 0.4f;
    public float pausaAntesDeRegresar = 0.25f;

    [Header("Rebote en paredes")]
    public bool rebotaEnParedes = true;
    public int rebotesMaximos = 1;

    public OpcionesProyectil Clonar() => (OpcionesProyectil)MemberwiseClone();
}