using UnityEngine;

/// <summary>Temporarily holds physical mass while buffering a clamped future weight.</summary>
public class MassLock : MonoBehaviour
{
    public const float Duration = 10;
    public bool Locked { get; private set; }
    public bool WaitingForSpace { get; private set; }
    public float Remaining { get; private set; }
    public int PendingWeight { get; private set; }
    public bool Applying { get; private set; }
    PlayerController player;
    LineRenderer ring;
    void Awake()
    {
        player = GetComponent<PlayerController>();
        var go = new GameObject("Mass lock contour");
        go.transform.SetParent(transform, false);
        ring = go.AddComponent<LineRenderer>();
        ring.sharedMaterial = Art.Material;
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 32;
        ring.startWidth = ring.endWidth = .035f;
        ring.sortingOrder = 10;
        ring.enabled = false;
    }
    public bool Activate()
    {
        if (GameFlow.InputBlocked || Locked) return false;
        PendingWeight = player.Weight;
        Remaining = Duration;
        Locked = true;
        WaitingForSpace = false;
        GameAudio.At("lock_arm", transform.position, .5f);
        return true;
    }
    public void Buffer(int amount)
    {
        int next = Mathf.Clamp(PendingWeight + amount, 0, 17);
        if (next != PendingWeight) GameAudio.At("lock_buffer", transform.position, .2f);
        PendingWeight = next;
    }
    public bool Release()
    {
        if (!Locked || GameFlow.InputBlocked) return false;
        Applying = true;
        bool applied;
        try { applied = player.ChangeWeight(PendingWeight - player.Weight, false); }
        finally { Applying = false; }
        if (!applied && !WaitingForSpace) GameAudio.At("lock_blocked", transform.position, .45f);
        if (applied) GameAudio.At(Remaining <= 0 ? "lock_expire" : "lock_release", transform.position, .5f);
        WaitingForSpace = !applied;
        if (applied) Clear();
        else Remaining = 0;
        return applied;
    }
    public void Clear()
    {
        Locked = WaitingForSpace = Applying = false;
        Remaining = 0;
        PendingWeight = player.Weight;
        if (ring) ring.enabled = false;
    }
    void Update()
    {
        if (GameFlow.InputBlocked || !Locked) return;
        if (Input.GetKeyDown(KeyCode.Q)) Release();
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked || !Locked) return;
        Remaining = Mathf.Max(0, Remaining - Time.fixedDeltaTime);
        if (Remaining <= 0) Release();
    }
    void LateUpdate()
    {
        ring.enabled = Locked;
        if (!Locked) return;
        var b = player.GetComponent<Collider2D>().bounds;
        var color = WaitingForSpace ? new Color(1, .65f, .2f) : new Color(.22f, .95f, .9f);
        ring.startColor = ring.endColor = color;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * Mathf.PI * 2 / ring.positionCount;
            ring.SetPosition(i, b.center + new Vector3(Mathf.Cos(a)*(b.extents.x+.13f), Mathf.Sin(a)*(b.extents.y+.13f)));
        }
    }
}
