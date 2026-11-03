using UnityEngine;

/// <summary>Body-controlled carriage with a stationary medium-body docking test.</summary>
[DefaultExecutionOrder(90)]
public class RailShuttle : LabDevice
{
    public float left = 9, right = 35, dock = 22, speed = 3;
    public bool Riding { get; private set; }
    public int BodySize { get; private set; } = -1;
    public float DockProgress { get; private set; }
    public bool Docked { get; private set; }
    public bool requireReturn;
    public bool TurnaroundReached { get; private set; }
    public bool ReturnLegStarted { get; private set; }
    // The generated sprite includes a narrow glow below the brass wheels.
    public const float WheelInset = .065f;
    public string Status => Docked ? "DOCK LINK STORED" : requireReturn && !TurnaroundReached ? "REACH THE FAR STOP" : requireReturn && !ReturnLegStarted ? "GET HEAVY / RETURN LEFT" : Riding && BodySize == 1 ? "BRAKE AT THE LIT DOCK" : "MASS RAIL SHUTTLE";
    void Start()
    {
        var cart = DeviceArt(1, transform.position, 4, 6, transform, "AdvancedDevices");
        GetComponent<BoxCollider2D>().size = new Vector2(4, cart.bounds.size.y);
        float trackTop = cart.bounds.min.y + WheelInset;
        Beam("Shuttle rail", new Vector3(left, trackTop-.02f), new Vector3(right, trackTop-.02f), .04f, amber);
        Art.Place("NewArt carriage track", Art.Cell("Props", 3, 2, 0), new Vector3((left + right)*.5f, trackTop-.075f), new Vector2(right-left+4,.15f), 2);
        foreach (float x in new[] { left, dock, right })
            WorldFixtures.DeckSupport(x, trackTop - .15f);
        Beam("Dock marker", new Vector3(dock - .65f, trackTop - .08f), new Vector3(dock + .65f, trackTop - .08f), .06f, cyan);
        if (requireReturn)
        {
            Beam("Turnaround marker",new Vector3(right-.7f,trackTop),new Vector3(right+.7f,trackTop),.08f,amber);
            GamePresentation.WorldLabel("FAR STOP / RETURN HEAVY",new Vector3(right,trackTop+3),amber);
        }
    }
    public void RestoreDock(float x, bool docked, bool terminal = false, bool reversed = false) { transform.position = new Vector3(x, transform.position.y, 0); GetComponent<Rigidbody2D>().position = transform.position; Docked = docked; DockProgress = docked ? 1 : 0; TurnaroundReached=terminal || docked; ReturnLegStarted=reversed || docked; }
    void FixedUpdate()
    {
        Anchor = transform.position + Vector3.up * 3;
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        var bounds = GetComponent<Collider2D>().bounds;
        var feet = p ? p.GetComponent<Collider2D>().bounds : new Bounds();
        Riding = p && Mathf.Abs(feet.min.y - bounds.max.y) < .16f && feet.center.x > bounds.min.x && feet.center.x < bounds.max.x && p.Body.linearVelocity.y < .2f;
        BodySize = Riding ? p.Size : -1;
        float direction = Riding ? (p.Size == 0 ? 1 : p.Size == 2 ? -1 : 0) : 0;
        float x = Mathf.Clamp(transform.position.x + direction * speed * Time.fixedDeltaTime, left, right);
        if(requireReturn && Riding && x>right-.7f) TurnaroundReached=true;
        if(requireReturn && TurnaroundReached && Riding && p.Size==2 && x<right-1) ReturnLegStarted=true;
        Vector2 delta = new Vector2(x - transform.position.x, 0);
        GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + delta);
        if (Riding) p.Body.position += delta;
        if (!Docked)
        {
            bool valid = Riding && p.Size == 1 && Mathf.Abs(x - dock) < .7f && Mathf.Abs(p.HorizontalInput) < .1f && (!requireReturn || ReturnLegStarted);
            DockProgress = valid ? Mathf.Min(1, DockProgress + Time.fixedDeltaTime / 1.2f) : 0;
            if (DockProgress >= 1) { Docked = true; GameAudio.At("shuttle_docked", transform.position, .5f); }
        }
    }
}
