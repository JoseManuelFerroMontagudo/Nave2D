using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Sierra")]
public class AtaqueSierra : AtaqueBase
{
    public GameObject prefabSierra;
    public float daño = 10f;
    public float escala = 1f;

    public override bool EsPermanente => true;

    public override GameObject Activar(ShipWeapons armas)
    {
        if (!prefabSierra) return null;
        var go = Instantiate(prefabSierra, armas.transform);
        go.transform.localPosition = Vector3.zero;
        if (go.TryGetComponent<Sierra>(out var s)) s.Initialize(daño, escala);
        return go;
    }

    public override void Actualizar(ShipWeapons armas, GameObject inst, bool boost)
    {
        if (inst && inst.TryGetComponent<Sierra>(out var s)) s.SetBoost(boost);
    }
}