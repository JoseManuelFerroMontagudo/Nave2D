using UnityEngine;

public class GridBackground2D : MonoBehaviour
{
    void Start()
    {
        int size = 100;
        Texture2D tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Color c = ((x + y) % 2 == 0) ? new Color(0.15f, 0.15f, 0.25f) : Color.black;
                tex.SetPixel(x, y, c);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64);
        GetComponent<SpriteRenderer>().sprite = sprite;
        GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
        GetComponent<SpriteRenderer>().size = new Vector2(200, 200);
    }
}