using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [HideInInspector] public float daño = 1f;
    public float velocidad = 6f;
    public float vidaUtil = 4f;

    void Start() => Destroy(gameObject, vidaUtil);
    void Update() => transform.Translate(Vector3.up * velocidad * Time.deltaTime);

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player")) Destroy(gameObject);
    }
}