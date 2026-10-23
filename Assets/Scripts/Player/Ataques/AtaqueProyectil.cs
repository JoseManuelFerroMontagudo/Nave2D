using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Proyectil")]
public class AtaqueProyectil : AtaqueBase
{
    public GameObject prefab;
    public float velocidad = 20f;
    public bool desdeCañones = true;

    public override void Ejecutar(ShipWeapons armas)
    {
        if (!prefab) return;
        if (desdeCañones)
            foreach (var c in armas.Cañones) armas.Disparar(prefab, velocidad, c);
        else
            armas.Disparar(prefab, velocidad, armas.transform);
    }
}