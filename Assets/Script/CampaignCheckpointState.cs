using System.Collections.Generic;
using UnityEngine;

/// <summary>Permanent puzzle accomplishments survive a checkpoint retry with their player.</summary>
public static class CampaignCheckpointState
{
    public class Device { public string key; public float[] value; }
    static string Key(Component c) => c.GetType().Name + ":" + c.name;
    public static Device[] Capture()
    {
        var states = new List<Device>();
        foreach (var relay in Object.FindObjectsByType<BodyRelay>()) states.Add(new Device { key = Key(relay), value = new[] { relay.Charged ? 1f : 0f } });
        foreach (var prism in Object.FindObjectsByType<MassPrism>()) states.Add(new Device { key = Key(prism), value = (float[])prism.Charge.Clone() });
        // Rail identity uses its fixed endpoints, not its moving transform.
        foreach (var rail in Object.FindObjectsByType<RailShuttle>()) states.Add(new Device { key = "Rail:" + rail.left + ":" + rail.right + ":" + rail.transform.position.y, value = new[] { rail.transform.position.x, rail.Docked ? 1f : 0f, rail.TurnaroundReached ? 1f : 0f, rail.ReturnLegStarted ? 1f : 0f } });
        return states.ToArray();
    }
    public static void Restore(Device[] states)
    {
        if (states == null) return;
        var lookup = new Dictionary<string, float[]>(); foreach (var s in states) lookup[s.key] = s.value;
        foreach (var relay in Object.FindObjectsByType<BodyRelay>()) if (lookup.TryGetValue(Key(relay), out var r)) relay.RestoreCharge(r[0] > .5f);
        foreach (var prism in Object.FindObjectsByType<MassPrism>()) if (lookup.TryGetValue(Key(prism), out var p)) for (int i = 0; i < 3; i++) prism.Charge[i] = p[i];
        foreach (var rail in Object.FindObjectsByType<RailShuttle>()) if (lookup.TryGetValue("Rail:" + rail.left + ":" + rail.right + ":" + rail.transform.position.y, out var v)) rail.RestoreDock(v[0], v[1] > .5f, v.Length>2 && v[2]>.5f, v.Length>3 && v[3]>.5f);
        Physics2D.SyncTransforms();
    }
}
