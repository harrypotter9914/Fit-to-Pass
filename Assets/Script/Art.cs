using System.Collections.Generic;
using UnityEngine;

/// <summary>Loads original generated atlases. Sprite rectangles are cached once, never allocated per frame.</summary>
public static class Art
{
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static Material material;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() { sprites.Clear(); textures.Clear(); material = null; }
    public static Material Material => material ? material : material = new Material(Shader.Find("Sprites/Default"));
    public static Texture2D Texture(string name)
    {
        if (!textures.TryGetValue(name, out var t) || !t)
        {
            t = Resources.Load<Texture2D>("Generated/" + name);
            textures[name] = t;
        }
        return t;
    }
    public static Sprite Cell(string name, int columns, int rows, int index, bool trim = true, float pixelsPerUnit = 100)
    {
        string key = name + ":" + index + ":" + pixelsPerUnit + ":" + trim;
        if (sprites.TryGetValue(key, out var result) && result)
            return result;
        var t = Texture(name);
        if (!t)
            return null;
        int width = t.width / columns, height = t.height / rows;
        Rect rect = new Rect(index % columns * width, (rows - 1 - index / columns) * height, width, height);
        if (name == "Props")
        {
            int[] xs = { 0, 640, 1120, 1536 };
            int c = index % 3;
            rect = new Rect(xs[c] * t.width / 1536f, index < 3 ? t.height * .6f : 0, (xs[c + 1] - xs[c]) * t.width / 1536f, index < 3 ? t.height * .4f : t.height * .6f);
            // The neighboring door overlaps the nominal bottom-left cell.
            // Isolate the crate before alpha trimming, otherwise the door's
            // edge expands the sprite bounds and makes the crate too narrow.
            if (index == 3)
                rect = new Rect(72 * t.width / 1536f, 70 * t.height / 1024f, 470 * t.width / 1536f, 460 * t.height / 1024f);
        }
        if (name == "AdvancedDevices")
        {
            Rect[] cells = { new Rect(0, 594, 695, 430), new Rect(695, 594, 841, 430), new Rect(0, 0, 768, 579), new Rect(768, 0, 768, 579) };
            var r = cells[index];
            rect = new Rect(r.x*t.width/1536f, r.y*t.height/1024f, r.width*t.width/1536f, r.height*t.height/1024f);
        }
        if (name == "InterfaceKit")
        {
            Rect[] pieces = { new Rect(52, 478, 750, 470), new Rect(870, 582, 646, 222), new Rect(78, 167, 896, 162), new Rect(1105, 128, 278, 264) };
            var r = pieces[index];
            rect = new Rect(r.x * t.width / 1536f, r.y * t.height / 1024f, r.width * t.width / 1536f, r.height * t.height / 1024f);
        }
        if (name == "Fixtures")
            rect = new Rect((index % 2) * t.width * .5f, index < 2 ? t.height * .35f : 0, t.width * .5f, t.height * (index < 2 ? .65f : .35f));
        if (trim && t.isReadable)
        {
            int x0 = (int)rect.x, y0 = (int)rect.y, x1 = (int)rect.xMax, y1 = (int)rect.yMax;
            int minX = x1, minY = y1, maxX = x0, maxY = y0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (t.GetPixel(x, y).a > .65f)
                    {
                        minX = Mathf.Min(x, minX);
                        maxX = Mathf.Max(x, maxX);
                        minY = Mathf.Min(y, minY);
                        maxY = Mathf.Max(y, maxY);
                    }
            if (maxX > minX && maxY > minY)
                rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        if (name == "TerrainV2")
        {
            // Isolated mip chains prevent fine ground details shimmering while
            // the camera pans, without bleeding from neighbouring atlas cells.
            var cell = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, true);
            cell.name = key + " / filtered terrain";
            cell.SetPixels(t.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
            cell.filterMode = FilterMode.Trilinear; cell.wrapMode = TextureWrapMode.Clamp;
            cell.Apply(true, false); t = cell; rect = new Rect(0, 0, cell.width, cell.height);
        }
        Vector4 border = Vector4.zero;
        if (name == "Props" && index == 0)
            border = new Vector4(rect.height * .5f, 0, rect.height * .5f, 0);
        if (name == "Fixtures" && index == 1)
            border = new Vector4(0, rect.width, 0, rect.width);
        result = Sprite.Create(t, rect, new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        result.name = key;
        sprites[key] = result;
        return result;
    }
    public static SpriteRenderer Place(string name, Sprite sprite, Vector3 position, Vector2 size, int order = 5, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent)
            go.transform.SetParent(parent, false);
        go.transform.position = position;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.sharedMaterial = Material;
        r.sortingOrder = order;
        if (sprite)
        {
            bool horizontal = sprite.name.StartsWith("Props:0:");
            bool vertical = sprite.name.StartsWith("Fixtures:1:");
            if (horizontal || vertical)
            {
                float scale = horizontal ? size.y / sprite.bounds.size.y : size.x / sprite.bounds.size.x;
                r.drawMode = SpriteDrawMode.Sliced;
                r.size = size / scale;
                go.transform.localScale = new Vector3(scale / (parent ? parent.lossyScale.x : 1), scale / (parent ? parent.lossyScale.y : 1), 1);
            }
            else
                go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x / (parent ? parent.lossyScale.x : 1), size.y / sprite.bounds.size.y / (parent ? parent.lossyScale.y : 1), 1);
        }
        return r;
    }
}
