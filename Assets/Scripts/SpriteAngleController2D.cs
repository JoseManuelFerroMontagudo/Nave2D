using UnityEngine;

public class SpriteBillboardAndSelector : MonoBehaviour
{
    [Tooltip("Transform del padre (Empty) que define la dirección frontal del objeto")]
    public Transform parentTransform;

    [Tooltip("Cámara principal (se asigna automáticamente si no se define)")]
    public Camera mainCamera;

    [Tooltip("Arreglo de 8 sprites ordenados: 0 = frontal, 1 = 45°, ... 7 = 315° (sentido horario)")]
    public Sprite[] sprites;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (parentTransform == null)
            parentTransform = transform.parent;
    }

    void Update()
    {
        // -------------------- 1. Billboarding 2D --------------------
        // Dirección desde el sprite hacia la cámara, proyectada en el plano XY
        Vector3 toCamera3D = mainCamera.transform.position - transform.position;
        Vector2 toCamera = new Vector2(toCamera3D.x, toCamera3D.y);

        if (toCamera.sqrMagnitude > 0.0001f)
        {
            // Queremos que la parte inferior del sprite (local -Y) apunte a la cámara,
            // es decir, el eje Y del sprite debe apuntar en dirección opuesta (-toCamera).
            transform.up = -toCamera.normalized;   // rota el sprite en el plano XY
        }

        // -------------------- 2. Selección del sprite --------------------
        // Dirección desde el padre hacia la cámara, en el plano XY
        Vector3 toCameraFromParent3D = mainCamera.transform.position - parentTransform.position;
        Vector2 toCameraFromParent = new Vector2(toCameraFromParent3D.x, toCameraFromParent3D.y);

        if (toCameraFromParent.sqrMagnitude < 0.0001f)
            return; // cámara justo encima del padre, evitar división por cero

        // Orientación del padre (su "adelante" en 2D, normalmente su eje Y)
        Vector2 forwardDir = parentTransform.up;   // Vector2 descarta Z

        // Ángulos respecto al eje X positivo
        float forwardAngle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg;
        float cameraAngle = Mathf.Atan2(toCameraFromParent.y, toCameraFromParent.x) * Mathf.Rad2Deg;

        // Ángulo en sentido HORARIO desde la dirección a la cámara hasta la orientación del padre
        float horarioAngle = (forwardAngle - cameraAngle + 180f) % 360f;

        // Redondear al sector de 45° más cercano (0..7)
        int sector = Mathf.RoundToInt(horarioAngle / 45f) % 8;

        // Mapeo: sector 0 → sprite 0, sector 1 → sprite 7, sector 2 → sprite 6, ...
        int spriteIndex = (8 - sector) % 8;

        // Asignar el sprite si existe
        if (sprites != null && sprites.Length == 8 && sprites[spriteIndex] != null)
        {
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }
}