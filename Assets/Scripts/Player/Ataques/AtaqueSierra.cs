using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Sierra")]
public class AtaqueSierra : AtaqueBase
{
    public GameObject prefabSierra;

    public override bool EsPermanente => true;

    public override GameObject Activar(ShipWeapons armas)
    {
        if (!prefabSierra) return null;
        var go = Instantiate(prefabSierra, armas.transform);
        go.transform.localPosition = Vector3.zero;
        return go;
    }

    public override void Actualizar(ShipWeapons armas, GameObject inst, bool boost)
    {
        if (inst && inst.TryGetComponent<Sierra>(out var s)) s.SetBoost(boost);
    }
}