using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] Image mapImage;
    [SerializeField] RectTransform dotsContainer;
    [SerializeField] GameObject dotPrefab;
    [SerializeField] float dotSize = 10f;

    [Header("Sprites")]
    [SerializeField] Sprite playerSprite, enemySprite, bossSprite, asteroidSprite;
    [SerializeField] bool showAsteroids = true;

    [Header("Modo")]
    [Tooltip("OFF = mapa completo norte-arriba. ON = radar centrado en player, player mira arriba.")]
    [SerializeField] bool centerOnPlayer = true;
    [Tooltip("Solo en modo radar: distancia de mundo que mapea al borde.")]
    [SerializeField] float radarRange = 10f;

    [Header("Orientación")]
    [SerializeField] FacingAxis facingAxis = FacingAxis.Up;

    [Header("Refresco")]
    [SerializeField] float refreshInterval = 0.1f;

    public enum FacingAxis { Up, Right }

    LineRenderer boundary;
    Transform player;
    float nextRefresh;
    readonly List<Image> pool = new List<Image>();
    readonly List<string> tags = new List<string>();
    int used;

    void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshInterval;

        if (!boundary) foreach (var lr in FindObjectsOfType<LineRenderer>()) if (lr.loop) { boundary = lr; break; }
        if (!boundary) return;
        if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
        if (!player) return;

        tags.Clear();
        tags.Add("Enemy"); tags.Add("Boss");
        if (showAsteroids) tags.Add("Asteroid");

        Refresh();
    }

    void Refresh()
    {
        used = 0;
        var mr = mapImage.rectTransform.rect;
        float uiR = Mathf.Min(mr.width, mr.height) * 0.5f;
        GetBoundary(out Vector3 center, out float worldR);

        float pAngle = FacingAngle(player);
        Vector3 origin = centerOnPlayer ? player.position : center;
        float rot = centerOnPlayer ? 90f - pAngle : 0f;
        float scale = uiR / (centerOnPlayer ? radarRange : worldR);

        // Player
        if (playerSprite)
        {
            var rt = GetDot(playerSprite).rectTransform;
            rt.anchoredPosition = centerOnPlayer ? Vector2.zero : (Vector2)(player.position - origin) * scale;
            rt.localRotation = Quaternion.Euler(0, 0, centerOnPlayer ? 180f : pAngle + 90f);
        }

        // Resto
        foreach (var tag in tags)
        {
            var sprite = SpriteFor(tag);
            if (!sprite) continue;
            foreach (var go in GameObject.FindGameObjectsWithTag(tag))
            {
                if ((go.transform.position - center).sqrMagnitude > worldR * worldR) continue;

                Vector2 v = Rotate(go.transform.position - origin, rot) * scale;
                if (centerOnPlayer && v.sqrMagnitude > uiR * uiR) v = v.normalized * uiR;

                float a = FacingAngle(go.transform);
                var rt = GetDot(sprite).rectTransform;
                rt.anchoredPosition = v;
                rt.localRotation = Quaternion.Euler(0, 0, centerOnPlayer ? a - pAngle + 180f : a + 90f);
            }
        }

        for (int i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
    }

    Image GetDot(Sprite sprite)
    {
        Image img;
        if (used < pool.Count) { img = pool[used]; img.gameObject.SetActive(true); }
        else
        {
            img = Instantiate(dotPrefab, dotsContainer).GetComponent<Image>();
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(dotSize, dotSize);
            pool.Add(img);
        }
        used++;
        img.sprite = sprite;
        return img;
    }

    void GetBoundary(out Vector3 center, out float radius)
    {
        int n = boundary.positionCount;
        center = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            var p = boundary.GetPosition(i);
            center += boundary.useWorldSpace ? p : boundary.transform.TransformPoint(p);
        }
        center /= Mathf.Max(n, 1);

        radius = 0f;
        for (int i = 0; i < n; i++)
        {
            var p = boundary.GetPosition(i);
            if (!boundary.useWorldSpace) p = boundary.transform.TransformPoint(p);
            radius += Vector3.Distance(p, center);
        }
        radius /= Mathf.Max(n, 1);
    }

    Sprite SpriteFor(string tag)
    {
        if (tag == "Player")   return playerSprite;
        if (tag == "Boss")     return bossSprite;
        if (tag == "Enemy")    return enemySprite;
        if (tag == "Asteroid") return asteroidSprite;
        return null;
    }

    float FacingAngle(Transform t)
    {
        Vector3 f = facingAxis == FacingAxis.Up ? t.up : t.right;
        return Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}