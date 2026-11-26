using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ConvoyFollower : MonoBehaviour
{
    [Header("Cuerpo")]
    public Transform head;
    public Transform[] segmentVariants;
    public int bodyCount = 10;
    public float spacing = 0.5f;

    [Header("Cola (escala progresiva)")]
    [Tooltip("Cantidad de segmentos finales que se irán reduciendo.")]
    public int tailSegments = 3;
    [Tooltip("Escala del primer segmento de cola (respecto a 1).")]
    public float tailScaleStart = 0.85f;
    [Tooltip("Escala del último segmento.")]
    public float tailScaleEnd = 0.35f;

    [Header("Movimiento")]
    public float sampleDistance = 0.05f, followSmooth = 15f;
    public float rotationSpeed = 720f, tangentDelta = 0.05f;

    [Header("Profundidad")]
    public Camera sortCamera;
    public float depthMultiplier = 100f;
    public int baseOrder = 10000;
    public bool sortHead = true;
    public Vector2 sortPointOffset;

    Transform[] bodyParts;
    SpriteRenderer[] bodyRenderers;
    SpriteRenderer headRenderer;
    readonly List<Vector2> path = new List<Vector2>();

    void Awake()
    {
        if (!sortCamera) sortCamera = Camera.main;
        if (!head) foreach (Transform c in transform)
            if (c.name.ToLowerInvariant().Contains("head")) { head = c; break; }
        GenerateBody();
    }

    [ContextMenu("Generar cuerpo ahora")]
    public void GenerateBody()
    {
        if (!head || segmentVariants == null || segmentVariants.Length == 0) return;

        var old = new List<GameObject>();
        foreach (Transform c in transform) if (c.name.StartsWith("Body_")) old.Add(c.gameObject);
        old.ForEach(g => { if (Application.isPlaying) Destroy(g); else DestroyImmediate(g); });

        bodyParts = new Transform[bodyCount];
        bodyRenderers = new SpriteRenderer[bodyCount];

        for (int i = 0; i < bodyCount; i++)
        {
            // Orden secuencial desde la lista
            var src = segmentVariants[i % segmentVariants.Length];
            var clone = Instantiate(src, head.position - head.up * spacing * (i + 1), head.rotation, transform);
            clone.gameObject.SetActive(true);
            clone.name = $"Body_{i:00}";

            // Escala progresiva en la cola
            clone.localScale = Vector3.one * GetTailScale(i);

            bodyParts[i] = clone;
            bodyRenderers[i] = clone.GetComponentInChildren<SpriteRenderer>(true);
        }

        foreach (var v in segmentVariants) if (v && v.gameObject.scene.IsValid()) v.gameObject.SetActive(false);

        headRenderer = head.GetComponentInChildren<SpriteRenderer>(true);
    }

    float GetTailScale(int index)
    {
        int tailCount = Mathf.Clamp(tailSegments, 0, bodyCount);
        if (tailCount == 0) return 1f;

        int tailStart = bodyCount - tailCount;
        if (index < tailStart) return 1f;

        // t: 0 en el primer segmento de cola, 1 en el último
        float t = tailCount == 1 ? 1f : (index - tailStart) / (float)(tailCount - 1);
        return Mathf.Lerp(tailScaleStart, tailScaleEnd, t);
    }

    void LateUpdate()
    {
        if (!head || bodyParts == null) return;

        // Camino
        if (path.Count == 0 || Vector2.Distance(head.position, path[0]) >= sampleDistance)
        {
            path.Insert(0, head.position);
            int max = Mathf.CeilToInt(bodyParts.Length * spacing / sampleDistance) + 20;
            if (path.Count > max) path.RemoveAt(path.Count - 1);
        }

        // Cuerpo: posición + rotación por tangente
        for (int i = 0; i < bodyParts.Length; i++)
        {
            if (!bodyParts[i]) continue;
            float dist = (i + 1) * spacing;
            var tgt = GetPoint(dist);
            var cur = bodyParts[i].position;
            bodyParts[i].position = Vector3.Lerp(cur, new Vector3(tgt.x, tgt.y, cur.z), followSmooth * Time.deltaTime);

            float d = Mathf.Min(tangentDelta, dist);
            Vector2 fwd = GetPoint(dist - d) - GetPoint(dist + d);
            if (fwd.sqrMagnitude < 1e-6f) continue;

            fwd.Normalize();
            var rot = Quaternion.Euler(0, 0, Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg - 90f);
            bodyParts[i].rotation = rotationSpeed <= 0f ? rot
                : Quaternion.RotateTowards(bodyParts[i].rotation, rot, rotationSpeed * Time.deltaTime);
        }

        // Profundidad
        if (!sortCamera) return;
        var cp = sortCamera.transform.position;
        var cf = sortCamera.transform.forward;
        int N = bodyParts.Length;

        for (int i = 0; i < N; i++)
        {
            var sr = bodyRenderers[i];
            if (!sr) continue;
            var sp = sr.transform.position + sr.transform.TransformVector(sortPointOffset);
            sr.sortingOrder = baseOrder - Mathf.RoundToInt(Vector3.Dot(sp - cp, cf) * depthMultiplier) + (N - i);
        }
        if (sortHead && headRenderer)
        {
            var sp = headRenderer.transform.position + headRenderer.transform.TransformVector(sortPointOffset);
            headRenderer.sortingOrder = baseOrder - Mathf.RoundToInt(Vector3.Dot(sp - cp, cf) * depthMultiplier) + (N + 1);
        }
    }

    Vector2 GetPoint(float dist)
    {
        if (path.Count == 0) return head.position;
        float acc = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float seg = Vector2.Distance(path[i], path[i + 1]);
            if (acc + seg >= dist) return Vector2.Lerp(path[i], path[i + 1], (dist - acc) / seg);
            acc += seg;
        }
        return path[path.Count - 1];
    }
}