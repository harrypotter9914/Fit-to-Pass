using UnityEngine;

/// <summary>A heavy landing stores energy. Bridge occupancy drains it by body mass.</summary>
public class KineticCapacitor : LabDevice
{
    public Vector3 bridgeStart, bridgeEnd;
    public float lifetime = 8;
    public float Energy { get; private set; }
    public float ImpactSpeed { get; private set; }
    public int LastSize { get; private set; } = -1;
    public bool Powered => Energy > 0;
    public bool Rejected => Time.time < feedbackUntil && LastSize != 2;
    public bool OnBridge { get; private set; }
    public float DrainRate { get; private set; } = 1;
    float feedbackUntil, pulse;
    SpriteRenderer core;
    Collider2D deck;
    SpriteRenderer[] segments;
    LineRenderer cable;
    public string Status => Rejected ? "HEAVY IMPACT NEEDED" : Powered ? "BRIDGE ENERGIZED" : "JUMP + LAND HEAVY";
    void Start()
    {
        Anchor = transform.position + Vector3.up * 2.5f;
        float floor = GetComponent<Collider2D>().bounds.min.y;
        core = FloorArt(0, floor, 2.8f);
        var coreBox = GetComponent<BoxCollider2D>();
        coreBox.size = new Vector2(coreBox.size.x, core.bounds.size.y);
        coreBox.offset = new Vector2(0, core.bounds.center.y - transform.position.y);
        var bridge = new GameObject("Temporary energy bridge");
        bridge.layer = 6;
        bridge.transform.SetParent(transform, true);
        bridge.transform.position = (bridgeStart + bridgeEnd) * .5f - Vector3.up * .13f;
        var box = bridge.AddComponent<BoxCollider2D>();
        box.size = new Vector2(bridgeEnd.x - bridgeStart.x, .26f);
        box.enabled = false;
        deck = box;
        int count = Mathf.CeilToInt(box.size.x / 3);
        segments = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            float width = box.size.x / count;
            var tile = Art.Cell("LabDevices", 2, 2, 3);
            float tileHeight = width * tile.bounds.size.y / tile.bounds.size.x;
            segments[i] = DeviceArt(3, new Vector3(bridgeStart.x + (i + .5f) * width, bridgeStart.y - tileHeight * .5f + .03f), width, 5, bridge.transform);
        }
        cable = Beam("Capacitor power cable", transform.position, bridgeStart, .055f, cyan);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        var p = collision.gameObject.GetComponent<PlayerController>();
        if (!p || GameFlow.InputBlocked || Mathf.Abs(p.transform.position.x - transform.position.x) > 1.6f || p.transform.position.y < transform.position.y + .5f) return;
        ImpactSpeed = Mathf.Abs(collision.relativeVelocity.y);
        if (ImpactSpeed < 2) return;
        LastSize = p.Size;
        feedbackUntil = Time.time + 2;
        if (p.Size != 2) { GameAudio.At("capacitor_reject", transform.position, .3f); return; }
        Energy = lifetime;
        pulse = .3f;
        GameAudio.At("capacitor_charge", transform.position, .6f);
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        OnBridge = p && p.transform.position.x > bridgeStart.x && p.transform.position.x < bridgeEnd.x && Mathf.Abs(p.transform.position.y - bridgeStart.y) < 4;
        DrainRate = OnBridge ? (p.Size == 0 ? 1 : p.Size == 1 ? 1.5f : 3) : 1;
        Energy = Mathf.Max(0, Energy - Time.fixedDeltaTime * DrainRate);
        deck.enabled = Powered;
        pulse = Mathf.Max(0, pulse - Time.fixedDeltaTime);
        core.color = Color.Lerp(Color.white, cyan, Reduced ? 0 : pulse * 2);
        Color c = Powered ? Energy < 2 ? amber : cyan : new Color(.3f, .65f, .7f, .16f);
        foreach (var segment in segments) segment.color = c;
        cable.startColor = cable.endColor = c;
    }
}
