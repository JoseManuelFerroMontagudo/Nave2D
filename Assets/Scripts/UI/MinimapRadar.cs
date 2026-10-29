using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapRadar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform dotsContainer;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private float dotSize = 10f;

    [Header("Sprites por tipo")]
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite bossSprite;

    [Header("Asteroids")]
    [SerializeField] private bool showAsteroids = true;
    [SerializeField] private Sprite asteroidSprite;

    [Header("Rango del radar")]
    [Tooltip("Distancia en world units que mapea al borde del minimapa.")]
    [SerializeField] private float maxWorldDistance = 10f;

    [Header("Orientación")]
    [Tooltip("Eje que representa el 'facing' del personaje. Normalmente Up en 2D.")]
    [SerializeField] private FacingAxis facingAxis = FacingAxis.Up;

    [Header("Refresco")]
    [SerializeField] private float refreshInterval = 0.1f;

    public enum FacingAxis { Up, Right }

    private LineRenderer boundary;
    private Transform player;
    private float nextRefresh;
    private readonly List<Image> dotPool = new List<Image>();
    private int usedDots;

    private readonly List<string> activeTags = new List<string>();
    private bool lastShowAsteroids;

    void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshInterval;

        if (boundary == null)
        {
            foreach (var lr in FindObjectsOfType<LineRenderer>())
            {
                if (lr.loop) { boundary = lr; break; }
            }
            if (boundary == null) return;
        }

        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
            if (player == null) return;
        }

        RebuildTagsIfNeeded();
        RefreshDots();
    }

    void RebuildTagsIfNeeded()
    {
        if (activeTags.Count > 0 && lastShowAsteroids == showAsteroids) return;

        lastShowAsteroids = showAsteroids;
        activeTags.Clear();
        activeTags.Add("Enemy");
        activeTags.Add("Boss");
        if (showAsteroids) activeTags.Add("Asteroid");
    }

    void RefreshDots()
    {
        usedDots = 0;

        Rect r = mapImage.rectTransform.rect;
        float uiRadius = Mathf.Min(r.width, r.height) * 0.5f;

        GetBoundary(out Vector3 worldCenter, out float worldRadius);

        // Ángulo del player en world (dirección a la que mira)
        Vector3 pFwd = facingAxis == FacingAxis.Up ? player.up : player.right;
        float playerWorldAngle = Mathf.Atan2(pFwd.y, pFwd.x) * Mathf.Rad2Deg;

        // ---- Player: siempre en el centro, siempre apuntando arriba ----
        if (playerSprite != null)
        {
            var pImg = GetDot();
            pImg.sprite = playerSprite;

            var prt = pImg.rectTransform;
            prt.anchoredPosition = Vector2.zero;
            // Sprite base apunta hacia abajo → 180° lo deja apuntando arriba
            prt.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        Vector3 playerPos = player.position;
        // Rotación que lleva el forward del player a "screen up" (90°)
        float rotateWorld = 90f - playerWorldAngle;

        // ---- Resto de objetos, relativos al player y rotados ----
        foreach (var tag in activeTags)
        {
            Sprite sprite = GetSpriteForTag(tag);
            if (sprite == null) continue;

            foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            {
                if (!IsInsideBoundary(go.transform.position, worldCenter, worldRadius))
                    continue;

                Vector3 delta3 = go.transform.position - playerPos;
                Vector2 delta = new Vector2(delta3.x, delta3.y);

                // Rotamos el delta para que el forward del player apunte arriba
                Vector2 rotated = Rotate(delta, rotateWorld);

                Vector2 uiPos = (rotated / maxWorldDistance) * uiRadius;
                if (uiPos.magnitude > uiRadius)
                    uiPos = uiPos.normalized * uiRadius;

                var img = GetDot();
                img.sprite = sprite;

                // Rotación del dot: facing del objeto relativo al player (+180 por sprite base -Y)
                Vector3 fwd = facingAxis == FacingAxis.Up ? go.transform.up : go.transform.right;
                float objWorldAngle = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg;
                float dotAngle = objWorldAngle - playerWorldAngle + 180f;

                var rt = img.rectTransform;
                rt.anchoredPosition = uiPos;
                rt.localRotation = Quaternion.Euler(0f, 0f, dotAngle);
            }
        }

        for (int i = usedDots; i < dotPool.Count; i++)
            dotPool[i].gameObject.SetActive(false);
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    bool IsInsideBoundary(Vector3 worldPos, Vector3 center, float radius)
    {
        Vector2 d = new Vector2(worldPos.x - center.x, worldPos.y - center.y);
        return d.magnitude <= radius;
    }

    Sprite GetSpriteForTag(string tag)
    {
        switch (tag)
        {
            case "Player": return playerSprite;
            case "Boss": return bossSprite;
            case "Enemy": return enemySprite;
            case "Asteroid": return asteroidSprite;
            default: return null;
        }
    }

    void GetBoundary(out Vector3 center, out float radius)
    {
        int n = boundary.positionCount;
        var pts = new Vector3[n];
        center = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = boundary.GetPosition(i);
            if (!boundary.useWorldSpace) p = boundary.transform.TransformPoint(p);
            pts[i] = p;
            center += p;
        }
        if (n > 0) center /= n;

        radius = 0f;
        for (int i = 0; i < n; i++) radius += Vector3.Distance(pts[i], center);
        if (n > 0) radius /= n;
    }

    Image GetDot()
    {
        if (usedDots < dotPool.Count)
        {
            var d = dotPool[usedDots++];
            d.gameObject.SetActive(true);
            return d;
        }

        var go = Instantiate(dotPrefab, dotsContainer);
        go.transform.localScale = Vector3.one;

        var img = go.GetComponent<Image>();
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dotSize, dotSize);

        dotPool.Add(img);
        usedDots++;
        return img;
    }
}