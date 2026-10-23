using UnityEngine;

public abstract class AtaqueBase : ScriptableObject
{
    public float cadencia = 10f;
    public float costoCalor = 0f;
    public int costoEnergia = 0;
    public int costoMunicion = 0;

    public virtual bool EsPermanente => false;

    // Instantáneo: dispara una vez
    public virtual void Ejecutar(ShipWeapons armas) { }

    // Permanente: se instancia al equipar, se destruye al desequipar
    public virtual GameObject Activar(ShipWeapons armas) => null;
    public virtual void Actualizar(ShipWeapons armas, GameObject inst, bool boost) { }
    public virtual void Tick(ShipWeapons armas, GameObject inst, bool boost) { }
}