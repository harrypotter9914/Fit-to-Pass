using UnityEngine;

/// <summary>Weight steers an optical router. Each receiver retains its completed charge.</summary>
public class MassPrism : LabDevice
{
    public int BodySize { get; private set; } = -1;
    public readonly float[] Charge = new float[3];
    public bool Complete => Charge[0] >= 1 && Charge[1] >= 1 && Charge[2] >= 1;
    public string Status => Complete ? "OPTICAL CIRCUIT COMPLETE" : "MASS PRISM ROUTER";
    LineRenderer ray;
    SpriteRenderer[] receivers = new SpriteRenderer[3];
    Vector3 emitter;
    void Start()
    {
        Anchor = transform.position + Vector3.up * 3;
        emitter = transform.position + new Vector3(3, 4);
        var prism = DeviceArt(2, emitter, 2.5f, 6, null, "AdvancedDevices");
        float pedestal = prism.bounds.min.y - transform.position.y;
        WorldFixtures.Fixture("Prism pedestal", 1, transform.position + new Vector3(3, pedestal * .5f), new Vector2(.6f, pedestal), 2);
        Art.Place("Prism bearing cap", Art.Cell("Props", 3, 2, 0),
            new Vector3(prism.bounds.center.x, prism.bounds.min.y - .045f), new Vector2(2.3f, .12f), 4, transform);
        FloorArt(0, transform.position.y, 2.5f, "AdvancedDevices", true);
        for (int i = 0; i < 3; i++)
        {
            receivers[i] = DeviceArt(3, emitter + new Vector3(5, (1-i)*3), 1.1f, 6, transform, "AdvancedDevices");
            var b = receivers[i].bounds;
            Art.Place("Receiver mounting shelf", Art.Cell("Props", 3, 2, 0), new Vector3(b.center.x, b.min.y-.07f), new Vector2(1.5f,.14f), 4, transform);
        }
        WorldFixtures.Fixture("Receiver support column", 1, transform.position + new Vector3(8, 3.5f), new Vector2(.38f, 7), 2);
        ray = Beam("Routed optical beam", emitter, emitter, .07f, cyan);
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        bool aboard = p && p.Grounded && Mathf.Abs(p.transform.position.x - transform.position.x) < 1.7f
            && Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y - transform.position.y) < .7f;
        BodySize = aboard ? p.Size : -1;
        ray.enabled = aboard;
        for (int i = 0; i < 3; i++)
        {
            if (Charge[i] < 1) Charge[i] = BodySize == i ? Mathf.Min(1, Charge[i] + Time.fixedDeltaTime / 1.5f) : 0;
            receivers[i].color = Charge[i] >= 1 ? cyan : Color.Lerp(new Color(.4f,.45f,.45f), Color.white, Charge[i]);
        }
        if (aboard) ray.SetPosition(1, receivers[BodySize].transform.position);
    }
}
