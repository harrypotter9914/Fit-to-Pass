using UnityEngine;

/// <summary>Applies airflow after player movement, never altering gravity outside the field.</summary>
[DefaultExecutionOrder(100)]
public class VectorAirwell : LabDevice
{
    public float height = 12, halfWidth = 1.9f;
    public bool tiered = true;
    // Heights refer to the player's feet, so a size change does not move a landing.
    public float highHeight = 9.6f, middleHeight = 4.7f, lowHeight = .55f;
    public float TargetFeet => transform.position.y + (BodySize == 0 ? highHeight : BodySize == 1 ? middleHeight : lowHeight);
    public bool Occupied { get; private set; }
    public int BodySize { get; private set; } = -1;
    public string Status => tiered ? "TIERED AIRFLOW" : "FREE FLIGHT AIRFLOW";
    LineRenderer[] streams;
    void Start()
    {
        Anchor = transform.position + Vector3.up * 2.8f;
        FloorArt(1, GetComponent<Collider2D>().bounds.min.y, 3.6f);
        streams = new LineRenderer[7];
        for (int i = 0; i < streams.Length; i++)
            streams[i] = Beam("Airflow tracer", transform.position, transform.position, .03f, new Color(cyan.r, cyan.g, cyan.b, .42f));
        Beam("Airwell rail left", transform.position + Vector3.left * 2.05f, transform.position + Vector3.left * 2.05f + Vector3.up * height, .035f, new Color(.4f, .8f, .8f, .2f));
        Beam("Airwell rail right", transform.position + Vector3.right * 2.05f, transform.position + Vector3.right * 2.05f + Vector3.up * height, .035f, new Color(.4f, .8f, .8f, .2f));
        if (tiered)
            foreach (float h in new[] { lowHeight, middleHeight, highHeight })
            {
                var at = transform.position + Vector3.up * h;
                Beam("Landing height marker", at + Vector3.left * 1.9f, at + Vector3.right * 2.6f, .045f, amber);
            }
        else
            for (float y = 2; y < height - 1; y += 2)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 at = transform.position + new Vector3(side * 2.05f, y);
                    Beam("Free flight chevron", at + new Vector3(-.18f, -.25f), at, .045f, cyan);
                    Beam("Free flight chevron", at, at + new Vector3(.18f, -.25f), .045f, cyan);
                }
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        Vector3 offset = p ? p.transform.position - transform.position : Vector3.zero;
        Occupied = p && Mathf.Abs(offset.x) < halfWidth && offset.y > .35f && offset.y < height;
        BodySize = Occupied ? p.Size : -1;
        if (Occupied)
        {
            // Tiered wells converge from either direction. Free-flight wells retain
            // continuous rise / stationary hover / descent, with the same controls.
            p.Body.gravityScale = 0;
            float feet = p.GetComponent<Collider2D>().bounds.min.y;
            float target = tiered ? Mathf.Clamp((TargetFeet - feet) * 4, -5.5f, 5.5f)
                : p.Size == 0 ? (offset.y > height - 1 ? 0 : 5.5f) : p.Size == 1 ? 0 : -4;
            p.Body.linearVelocity = new Vector2(p.Body.linearVelocity.x, Mathf.MoveTowards(p.Body.linearVelocity.y, target, Time.fixedDeltaTime * 30));
        }
        for (int i = 0; i < streams.Length; i++)
        {
            float y = Reduced ? height * .5f : Mathf.Repeat(Time.time * 3 + i * 1.7f, height - 1);
            Vector3 a = transform.position + new Vector3(-1.5f + i * .5f, y + .5f);
            streams[i].SetPosition(0, a);
            streams[i].SetPosition(1, a + Vector3.up * .8f);
        }
    }
}
