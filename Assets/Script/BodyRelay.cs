using UnityEngine;

public class BodyRelay : LabDevice
{
    public int requiredSize;
    public int RouteNumber;
    public string RouteLabel="";
    public bool Charged { get; private set; }
    public float Progress { get; private set; }
    public string Status => Charged ? "ROUTE RELAY STORED" : "ROUTE RELAY / " + new[] { "S", "M", "L" }[requiredSize];
    SpriteRenderer art;
    void Start()
    {
        Anchor = transform.position + Vector3.up * 3;
        art = FloorArt(3, transform.position.y, 1.2f, "AdvancedDevices");
        var bridge=GetComponentInParent<BalanceBridge>();
        if(bridge)
        {
            var shape=art.sprite.bounds;
            var foot=art.transform.TransformPoint(new Vector3(shape.center.x,shape.min.y,shape.center.z));
            art.transform.position+=transform.position-foot;
            art.GetComponent<GroundedFixture>().FollowSupport(bridge.transform,transform.position);
        }
    }
    public void RestoreCharge(bool charged) { Charged = charged; Progress = charged ? 1 : 0; if (art) art.color = charged ? cyan : Color.white; }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked || Charged) return;
        var p = GameFlow.Instance.Player;
        bool valid = p && p.Size == requiredSize && p.Grounded && Mathf.Abs(p.transform.position.x - transform.position.x) < 1.6f
            && Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y - transform.position.y) < .7f;
        Progress = valid ? Mathf.Min(1, Progress + Time.fixedDeltaTime) : 0;
        if (Progress >= 1) { Charged = true; art.color = cyan; GameAudio.At("relay_complete", transform.position, .5f); }
    }
}
