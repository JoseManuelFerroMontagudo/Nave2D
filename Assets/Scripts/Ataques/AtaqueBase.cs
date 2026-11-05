using System.Collections;
using UnityEngine;

public enum ModoSeparacion { Distancia, Angulo }

public abstract class AtaqueBase : ScriptableObject
{
    [Header("Común")]
    public float cadencia = 10f;
    public float costoCalor = 0f;
    public int costoEnergia = 0;
    public int costoMunicion = 0;

    [Header("UI")]
    public string nombreVisible;
    public Sprite icono;
    [TextArea] public string descripcion;

    [Header("Cañones / Dispersión")]
    [Tooltip("1 = desde el centro. 2+ = múltiples orígenes o direcciones.")]
    public int cantidadCañones = 1;
    [Tooltip("Distancia entre cañones adyacentes (m) o grados entre disparos adyacentes.")]
    public float separacion = 0f;
    public ModoSeparacion modoSeparacion = ModoSeparacion.Distancia;

    public virtual bool EsPermanente => false;

    public virtual IEnumerator Ejecutar(AtaqueContext ctx) { yield break; }
    public virtual GameObject Activar(AtaqueContext ctx) => null;
    public virtual void Actualizar(AtaqueContext ctx, GameObject inst, bool boost) { }
    public virtual void Tick(AtaqueContext ctx, GameObject inst, bool boost) { }
}