using UnityEngine;

public class AtaqueContext
{
    public Transform owner;
    public Transform origen;
    public Transform objetivo;
    public Vector2 forward;
    public Stats stats;

    Vector3 GetOrigen() => origen != null ? origen.position : owner.position;

    public void Disparar(GameObject prefab, int i, int total, float separacion, ModoSeparacion modo)
    {
        ObtenerOrigenYDir(i, total, separacion, modo, out var pos, out var dir);
        Disparar(prefab, pos, dir);
    }

    public void Disparar(GameObject prefab, Vector3 pos, Vector2 dir)
    {
        if (!prefab) return;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : forward;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, ang));

        if (go.TryGetComponent(out Projectile p)) p.Initialize(dir);
        else if (go.TryGetComponent(out Bullet b)) b.Configurar(dir);
    }

    void ObtenerOrigenYDir(int i, int total, float sep, ModoSeparacion modo,
                           out Vector3 pos, out Vector2 dir)
    {
        if (total < 1) total = 1;
        Vector3 basePos = GetOrigen();

        if (modo == ModoSeparacion.Distancia)
        {
            Vector2 right = new Vector2(forward.y, -forward.x);
            float offset = total > 1 ? (i - (total - 1) * 0.5f) * sep : 0f;
            pos = basePos + (Vector3)(right * offset);
            dir = forward;
        }
        else
        {
            pos = basePos;
            float angOffset = total > 1 ? (i - (total - 1) * 0.5f) * sep : 0f;
            float baseAng = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg + angOffset;
            float rad = baseAng * Mathf.Deg2Rad;
            dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}