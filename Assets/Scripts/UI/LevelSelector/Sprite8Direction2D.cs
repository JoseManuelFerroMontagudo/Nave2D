using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector de sprite 8 direcciones para UI Image top-down.
/// El PADRE rota; el hijo (esta Image) contrarresta la rotación heredada
/// para quedar recto respecto al Canvas, y solo cambia el sprite.
///
/// sprites[0]=arriba, [1]=arriba-dcha, [2]=dcha, [3]=abajo-dcha,
/// [4]=abajo, [5]=abajo-izq, [6]=izq, [7]=arriba-izq (sentido horario).
/// </summary>
[RequireComponent(typeof(Image))]
public class Sprite8Direction2D : MonoBehaviour
{
    [Header("Sprites (0=arriba, sentido horario)")]
    public Sprite[] sprites;

    [Header("Referencia de orientación")]
    [Tooltip("Vector 'arriba' de referencia. Vacío = Canvas.up (o Camera.up si no hay Canvas).")]
    public Transform reference;

    [Header("Fuente de la dirección")]
    [Tooltip("Transform que define la dirección a leer. Vacío = transform.parent (o el propio si no hay padre).")]
    public Transform directionSource;

    [Tooltip("Offset en grados para corregir la dirección leída si el sprite del padre está dibujado mirando a otro lado.")]
    public float directionOffset = 0f;

    [Header("Contrarrotación")]
    [Tooltip("Si está activo, la Image contrarresta la rotación heredada del padre y queda recta respecto al Canvas.")]
    public bool keepUpright = true;

    Image uiImage;
    RectTransform canvasRT;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRT = canvas.transform as RectTransform;
    }

    void LateUpdate() // LateUpdate: el padre ya rotó este frame
    {
        if (sprites == null || sprites.Length != 8) return;

        // 1) Dirección: leer del PADRE (el hijo va a estar contrarrotado)
        Transform dirT = directionSource != null
            ? directionSource
            : (transform.parent != null ? transform.parent : transform);

        Vector2 forward = dirT.up;
        if (directionOffset != 0f)
            forward = Quaternion.Euler(0f, 0f, directionOffset) * forward;
        if (forward.sqrMagnitude < 0.0001f) return;

        // 2) Referencia "arriba" (Canvas → pantalla; fallback a Camera.up o world up)
        Vector2 refUp = Vector2.up;
        if (reference != null) refUp = reference.up;
        else if (canvasRT != null) refUp = canvasRT.up;
        else if (Camera.main != null)
        {
            var c = Camera.main.transform.up;
            var cand = new Vector2(c.x, c.y);
            if (cand.sqrMagnitude > 0.0001f) refUp = cand.normalized;
        }

        // 3) Sector horario desde 'refUp' hasta 'forward'
        float refAng = Mathf.Atan2(refUp.y, refUp.x) * Mathf.Rad2Deg;
        float fwdAng = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float clockwise = -Mathf.DeltaAngle(refAng, fwdAng);
        if (clockwise < 0f) clockwise += 360f;

        int sector = Mathf.RoundToInt(clockwise / 45f) % 8;
        Sprite s = sprites[sector];
        if (s == null) return;

        uiImage.sprite = s;

        // 4) Contrarrestar la rotación heredada en espacio de Canvas
        if (keepUpright)
        {
            // rotation (world), NO localRotation: así se cancela la rotación del padre.
            transform.rotation = canvasRT != null ? canvasRT.rotation : Quaternion.identity;
        }
    }
}