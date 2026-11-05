using UnityEngine;
public interface IPermanenteHandler
{
    void SetBoost(bool boost);
}

[CreateAssetMenu(menuName = "Ataques/Permanente")]
public class AtaquePermanente : AtaqueBase
{
    public GameObject prefab;

    public override bool EsPermanente => true;

    public override GameObject Activar(AtaqueContext ctx)
    {
        if (!prefab) return null;
        var go = Object.Instantiate(prefab, ctx.owner);
        go.transform.localPosition = Vector3.zero;
        return go;
    }

    public override void Actualizar(AtaqueContext ctx, GameObject inst, bool boost)
    {
        if (inst && inst.TryGetComponent<IPermanenteHandler>(out var h))
            h.SetBoost(boost);
    }
}