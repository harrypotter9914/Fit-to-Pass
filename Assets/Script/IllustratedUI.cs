using UnityEngine;

/// <summary>Uniform illustration fitting and fixed-shape frame corners.</summary>
public static class IllustratedUI
{
    public static Rect Fit(Rect box, Vector2 sourceSize)
    {
        float scale = Mathf.Min(box.width / sourceSize.x, box.height / sourceSize.y);
        Vector2 size = sourceSize * scale;
        return new Rect(box.center - size * .5f, size);
    }
    public static void Image(Rect box, Sprite sprite)
    {
        if (!sprite)
            return;
        Blit(Fit(box, sprite.rect.size), sprite.texture, sprite.rect);
    }
    static void Blit(Rect destination, Texture2D texture, Rect pixels)
        => GUI.DrawTextureWithTexCoords(destination, texture, new Rect(pixels.x / texture.width, pixels.y / texture.height, pixels.width / texture.width, pixels.height / texture.height));

    // Only undecorated straight strips and the blank center change length.
    // Every corner (including its circular screw) has the same X/Y scale.
    public static void Frame(Rect box, Sprite sprite, bool modal)
    {
        if (!sprite)
            return;
        Rect r = sprite.rect;
        float sourceCorner = Mathf.Min(r.width, r.height) * (modal ? .12f : .42f);
        float corner = Mathf.Min(modal ? 48 : 30, Mathf.Min(box.width, box.height) * .45f);
        float[] dx = { box.xMin, box.xMin + corner, box.xMax - corner, box.xMax };
        float[] dy = { box.yMin, box.yMin + corner, box.yMax - corner, box.yMax };
        float[] sx = { r.xMin, r.xMin + sourceCorner, r.xMax - sourceCorner, r.xMax };
        float[] sy = { r.yMax, r.yMax - sourceCorner, r.yMin + sourceCorner, r.yMin };
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                // GUI.matrix can put a shared edge between physical pixels.
                // Snap and overlap by half a pixel to avoid sampling cracks.
                float scale = GUI.matrix.m00;
                float left = (Mathf.Round(dx[x] * scale + GUI.matrix.m03) - GUI.matrix.m03) / scale;
                float top = (Mathf.Round(dy[y] * scale + GUI.matrix.m13) - GUI.matrix.m13) / scale;
                float right = (Mathf.Round(dx[x + 1] * scale + GUI.matrix.m03) - GUI.matrix.m03) / scale;
                float bottom = (Mathf.Round(dy[y + 1] * scale + GUI.matrix.m13) - GUI.matrix.m13) / scale;
                Blit(new Rect(left, top, right - left + .5f / scale, bottom - top + .5f / scale), sprite.texture,
                    new Rect(sx[x], sy[y + 1], sx[x + 1] - sx[x], sy[y] - sy[y + 1]));
            }
    }
}
