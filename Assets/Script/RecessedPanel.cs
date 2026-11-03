using UnityEngine;

/// <summary>A shutter slides behind a fixed frame; neither art nor collision enters the next floor.</summary>
public class RecessedPanel : MonoBehaviour
{
    public Bounds Enclosure { get; private set; }
    BoxCollider2D box;
    Vector2 originalSize, originalOffset;
    Vector3 closed;
    GameObject maskObject;
    Sprite maskSprite;
    public void Initialize(Vector3 closedPosition)
    {
        closed = closedPosition; box = GetComponent<BoxCollider2D>();
        originalSize = box.size; originalOffset = box.offset; Enclosure = box.bounds;
        maskObject = new GameObject("Fixed shutter recess"); maskObject.transform.position = Enclosure.center;
        var texture = Texture2D.whiteTexture;
        maskSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f,.5f), 1);
        var mask = maskObject.AddComponent<SpriteMask>(); mask.sprite = maskSprite;
        mask.isCustomRangeActive = true; mask.backSortingOrder = 3; mask.frontSortingOrder = 6;
        maskObject.transform.localScale = new Vector3(Enclosure.size.x / texture.width, Enclosure.size.y / texture.height, 1);
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
    public void Refresh()
    {
        float travel = Mathf.Clamp(transform.position.y - closed.y, 0, originalSize.y);
        float height = originalSize.y - travel;
        box.enabled = height > .005f;
        if (box.enabled)
        {
            box.size = new Vector2(originalSize.x, height);
            box.offset = originalOffset - Vector2.up * travel * .5f;
        }
    }
    void OnDestroy()
    {
        if (maskObject) Destroy(maskObject);
        if (maskSprite) Destroy(maskSprite);
    }
}
