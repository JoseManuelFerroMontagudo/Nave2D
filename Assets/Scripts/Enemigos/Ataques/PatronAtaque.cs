using UnityEngine;
using System.Collections;

public abstract class PatronAtaque : ScriptableObject
{
    [Header("Proyectil")]
    public GameObject prefabBullet;
    public int daño = 5;
    public float velocidad = 6f;
    public float vidaUtil = 4f;

    public abstract IEnumerator Ejecutar(Transform origen, Transform objetivo);

    protected void Disparar(Transform origen, Vector2 dir)
    {
        if (prefabBullet == null) return;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        var go = Instantiate(prefabBullet, origen.position, Quaternion.Euler(0, 0, ang));
        if (go.TryGetComponent(out Bullet b))
        {
            b.daño = daño;
            b.velocidad = velocidad;
            b.vidaUtil = vidaUtil;
        }
    }
}