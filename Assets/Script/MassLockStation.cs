using UnityEngine;

/// <summary>A grounded optional preparation station, used before wind-shaft crossings.</summary>
public class MassLockStation : LabDevice
{
    public bool InRange { get; private set; }
    SpriteRenderer art;
    void Start()
    {
        Anchor = transform.position + Vector3.up * 3;
        var sprite = Art.Cell("MassLockStation", 1, 1, 0);
        float height = 3 * sprite.bounds.size.y / sprite.bounds.size.x;
        art = WorldFixtures.OnFloor("NewArt Mass lock station", sprite, transform.position.x,
            transform.position.y, new Vector2(3, height), 4, transform);
        // A low step supports the player; the open arms and field are passable.
        gameObject.layer = 6;
        var pad = gameObject.AddComponent<BoxCollider2D>();
        pad.size = new Vector2(2.65f, height * .27f);
        pad.offset = Vector2.up * pad.size.y * .5f;
    }
    bool CanReach(PlayerController p) => p && p.Grounded &&
        Mathf.Abs(p.transform.position.x-transform.position.x)<1.8f &&
        Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-transform.position.y)<.7f;
    public bool Activate()
    {
        var p = GameFlow.Instance.Player;
        if (GameFlow.InputBlocked || !CanReach(p)) return false;
        var hold = p.GetComponent<MassLock>();
        if (!hold) hold = p.gameObject.AddComponent<MassLock>();
        return hold.Activate();
    }
    void Update()
    {
        if (GameFlow.InputBlocked) return;
        InRange = CanReach(GameFlow.Instance.Player);
        if (InRange && Input.GetKeyDown(KeyCode.E)) Activate();
        art.color = InRange ? new Color(.7f, 1, .97f) : Color.white;
    }
}
