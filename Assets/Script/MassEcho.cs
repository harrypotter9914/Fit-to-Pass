using UnityEngine;

/// <summary>Stores a heavy body as a temporary holographic load, freeing its owner to shrink.</summary>
public class MassEcho : LabDevice
{
    public float duration = 18;
    public float Remaining { get; private set; }
    public bool Powered => Remaining > 0;
    public bool InRange { get; private set; }
    public bool Rejected { get; private set; }
    SpriteRenderer echo;
    public string Status => Powered ? "E / REFRESH HEAVY ECHO" : Rejected ? "BECOME HEAVY, THEN E" : "HEAVY + E / RECORD ECHO";
    void Start()
    {
        Anchor = transform.position + Vector3.up * 3;
        FloorArt(0, transform.position.y, 2.8f, "AdvancedDevices", true);
        var sprite = Art.Cell("Subject", 4, 2, 0, false, 300);
        echo = Art.Place("Recorded mass hologram", sprite, transform.position + Vector3.up * 2,
            new Vector2(2.8f * sprite.bounds.size.x / sprite.bounds.size.y, 2.8f), 8, transform);
        echo.color = new Color(.2f, 1, .9f, .45f);
        echo.enabled = false;
    }
    void Update()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        InRange = p && Mathf.Abs(p.transform.position.x - transform.position.x) < 2 && p.Grounded
            && Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y - transform.position.y) < .8f;
        if (Input.GetKeyDown(KeyCode.E)) Record();
        Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
        echo.enabled = Powered;
        echo.color = new Color(.2f, 1, .9f, Reduced ? .45f : .4f + Mathf.Sin(Time.time * 3) * .1f);
    }
    public bool Record()
    {
        if (GameFlow.InputBlocked || !InRange) return false;
        Rejected = GameFlow.Instance.Player.Size != 2;
        if (Rejected) { GameAudio.At("echo_reject", transform.position, .3f); return false; }
        Remaining = duration;
        GameAudio.At("echo_record", transform.position, .5f);
        return true;
    }
}
