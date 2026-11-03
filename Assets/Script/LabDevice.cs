using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared presentation for physical lab devices. State remains in each device.</summary>
public abstract class LabDevice : MonoBehaviour
{
    public static readonly List<LabDevice> Active = new List<LabDevice>();
    public Vector3 Anchor { get; protected set; }
    protected readonly Color cyan = new Color(.22f, .95f, .9f);
    protected readonly Color amber = new Color(1, .65f, .2f);
    protected readonly Color red = new Color(1, .28f, .2f);
    protected bool Reduced => PlayerPrefs.GetInt("Fit.ReducedMotion", 0) == 1;
    protected virtual void OnEnable() => Active.Add(this);
    protected virtual void OnDisable() => Active.Remove(this);
    protected SpriteRenderer DeviceArt(int cell, Vector3 position, float width, int order = 6, Transform parent = null, string atlas = "LabDevices")
    {
        var sprite = Art.Cell(atlas, 2, 2, cell);
        return Art.Place("NewArt Lab device", sprite, position,
            new Vector2(width, width * sprite.bounds.size.y / sprite.bounds.size.x), order, parent);
    }
    protected SpriteRenderer FloorArt(int cell, float floor, float width, string atlas = "LabDevices", bool solidPad = false)
    {
        var sprite = Art.Cell(atlas, 2, 2, cell);
        var art = WorldFixtures.OnFloor("NewArt Lab device", sprite, transform.position.x, floor,
            new Vector2(width, width * sprite.bounds.size.y / sprite.bounds.size.x), 6, transform);
        if (solidPad)
        {
            gameObject.layer = 6;
            var pad = gameObject.AddComponent<BoxCollider2D>();
            // This atlas includes the hologram above the solid emitter. The
            // walkable rim is 22% of the drawn width above its floor contact.
            float rimHeight = width * .22f;
            pad.size = new Vector2(width * .88f, rimHeight);
            pad.offset = new Vector2(0, floor - transform.position.y + rimHeight * .5f);
        }
        return art;
    }
    protected LineRenderer Beam(string name, Vector3 a, Vector3 b, float width, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Art.Material;
        line.sortingOrder = 9;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.SetPosition(0, a); line.SetPosition(1, b);
        return line;
    }
}
