using UnityEngine;

/// <summary>Stationary body scan with visible rejection and a continuous opening stroke.</summary>
public class MassGate : LabDevice
{
    public int requiredSize;
    public bool Open { get; private set; }
    public float Progress { get; private set; }
    public bool InRange { get; private set; }
    public bool WrongSize { get; private set; }
    public bool Moving { get; private set; }
    public int ObservedSize { get; private set; } = -1;
    Vector3 closed;
    LineRenderer scanner;
    float soundAfter;
    RecessedPanel recess;
    public string Status => Open ? "ACCESS GRANTED" : !InRange ? "BODY SCANNER" : WrongSize ? "SIZE MISMATCH" : Moving ? "HOLD STILL" : "SCANNING";
    void Start()
    {
        closed = transform.position;
        recess = gameObject.AddComponent<RecessedPanel>();
        recess.Initialize(closed);
        Anchor = closed + Vector3.up * 2.35f;
        var sensor = Art.Cell("LabDevices", 2, 2, 2);
        float floor = GetComponent<Collider2D>().bounds.min.y;
        foreach (float side in new[] { -1f, 1f })
            WorldFixtures.OnFloor("NewArt Scanner column", sensor, closed.x + side * 1.25f, floor,
                new Vector2(2 * sensor.bounds.size.x / sensor.bounds.size.y, 2), 6);
        scanner = Beam("Body scan beam", closed, closed, .045f, cyan);
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        InRange = p && Mathf.Abs(p.transform.position.x - closed.x) < 2.8f && Mathf.Abs(p.transform.position.y - closed.y) < 2.6f;
        ObservedSize = InRange ? p.Size : -1;
        WrongSize = InRange && p.Size != requiredSize;
        Moving = InRange && (Mathf.Abs(p.Body.linearVelocity.x) > .2f || !p.Grounded);
        if (!Open)
        {
            bool valid = InRange && !WrongSize && !Moving && p.Grounded;
            Progress = valid ? Mathf.Min(1, Progress + Time.fixedDeltaTime / 1.2f) : 0;
            if (WrongSize && Time.time > soundAfter)
            {
                GameAudio.At("scanner_reject", transform.position, .3f);
                soundAfter = Time.time + 2;
            }
            if (Progress >= 1)
            {
                Open = true;

            }
        }
        if (Open)
            transform.position = Vector3.MoveTowards(transform.position, closed + Vector3.up * 3.6f, Time.fixedDeltaTime * 3);
        recess.Refresh();
        float sweep = Reduced ? .5f : Mathf.PingPong(Time.time * .75f, 1);
        float y = closed.y - 1.5f + sweep * 3;
        scanner.SetPosition(0, new Vector3(closed.x - 1.15f, y, 0));
        scanner.SetPosition(1, new Vector3(closed.x + 1.15f, y, 0));
        scanner.startColor = scanner.endColor = WrongSize && !Open ? red : cyan;
        scanner.enabled = InRange && !Open;
    }
}
