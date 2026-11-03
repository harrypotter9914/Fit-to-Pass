using UnityEngine;

/// <summary>Full body clearance on a supported floor, independent of current size.</summary>
public static class RespawnSafety
{
    static void Shape(PlayerController p, int size, out Vector2 dimensions, out Vector2 offset)
    {
        var scale3 = size == 0 ? p.smallScale : size == 2 ? p.largeScale : p.normalScale;
        var scale = new Vector2(Mathf.Abs(scale3.x), Mathf.Abs(scale3.y));
        if (p.TryGetComponent<BoxCollider2D>(out var box))
        {
            dimensions=Vector2.Scale(box.size,scale);
            offset=Vector2.Scale(box.offset,scale);
            return;
        }
        var b = p.GetComponent<Collider2D>().bounds;
        float ratio = scale.y / Mathf.Abs(p.transform.localScale.y);
        dimensions = (Vector2)b.size * ratio;
        offset = Vector2.Scale(p.GetComponent<Collider2D>().offset,scale);
    }
    public static Vector3 Root(PlayerController p, Vector2 footing, int size)
    {
        Shape(p, size, out var dimensions, out var offset);
        return new Vector3(footing.x-offset.x, footing.y + .04f + dimensions.y*.5f-offset.y, 0);
    }
    public static bool Clear(PlayerController p, Vector3 root, int size)
    {
        Shape(p, size, out var dimensions, out var offset);
        foreach (var c in Physics2D.OverlapBoxAll((Vector2)root+offset, dimensions-new Vector2(.025f,.035f), 0))
            if (c && c.enabled && c.gameObject.activeInHierarchy && !c.GetComponentInParent<PlayerController>() &&
                (!c.isTrigger || c.CompareTag("Death") || c.GetComponent<DeathZone>())) return false;
        return true;
    }
    public static bool Find(PlayerController p, Vector2 footing, int size, out Vector3 root)
    {
        for (int i=0; i<=48; i++)
        {
            float dx = i==0 ? 0 : ((i+1)/2)*.5f*(i%2==1?-1:1);
            float x = footing.x+dx;
            root = Root(p, new Vector2(x, footing.y), size);
            if (WorldFixtures.HasTerrainContact(x, footing.y) && WorldFixtures.HasTerrainContact(x-.32f, footing.y) &&
                WorldFixtures.HasTerrainContact(x+.32f, footing.y) && Clear(p, root, size)) return true;
        }
        root = default; return false;
    }
    public static bool FromSavedRoot(PlayerController p, Vector3 saved, int size, out Vector3 root)
    {
        Shape(p, size, out var dimensions, out var offset);
        return Find(p, new Vector2(saved.x+offset.x, saved.y+offset.y-dimensions.y*.5f-.04f), size, out root);
    }
}
