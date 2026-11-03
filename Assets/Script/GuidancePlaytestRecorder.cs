#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Read-only playtest observer. Never moves the actor or changes game state.</summary>
public class GuidancePlaytestRecorder : MonoBehaviour
{
    public static GuidancePlaytestRecorder Instance;
    public string Folder;
    public int Samples, PlayerOverlap, DeviceOverlap, Unreadable, Presentations;
    float next, changed, lastCapture = -10;
    TutorialPoint current;
    int serial, lastStep = -1;
    bool captured;
    readonly HashSet<string> pictures = new HashSet<string>();
    [Serializable] class Row
    {
        public int chapter, step, phase;
        public float time, x, y, alpha;
        public string topic, owner, caption, image;
        public bool compact, playerOverlap, deviceOverlap, textFits;
    }
    public static void Begin(string folder)
    {
        if (Instance) Destroy(Instance.gameObject);
        var go = new GameObject("Guidance playtest observer");
        Instance = go.AddComponent<GuidancePlaytestRecorder>();
        Instance.Folder = folder; Directory.CreateDirectory(folder);
    }
    void LateUpdate()
    {
        if (Time.unscaledTime < next || !GameFlow.Instance || GameFlow.Instance.Loading) return;
        next = Time.unscaledTime + .12f;
        var flow = GameFlow.Instance; var p = flow.Player; var g = EnvironmentalGuidance.Instance;
        if (!p || !g || !g.View || flow.Screen != FlowScreen.Playing) return;
        if (current != g.ActivePoint) { current = g.ActivePoint; changed = Time.time; captured = false; Presentations++; }
        float alpha = g.View.GetComponent<CanvasGroup>().alpha;
        if (alpha < .5f) return;
        Samples++;
        var r = g.Placement; var b = new Bounds(r.center, new Vector3(r.width, r.height, 2));
        var pb = p.GetComponent<Collider2D>().bounds;
        bool po = b.Intersects(pb);
        bool device = UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r => r.enabled && r.gameObject.activeInHierarchy && r.sortingOrder>=4 && !r.GetComponentInParent<PlayerController>() && b.Intersects(r.bounds));
        PlayerOverlap += po ? 1 : 0; DeviceOverlap += device ? 1 : 0; Unreadable += !g.View.TextFits ? 1 : 0;
        var row = new Row { chapter = flow.Stage, time = flow.Elapsed, x = p.transform.position.x, y = p.transform.position.y,
            topic = g.ActiveId, owner = g.ActivePoint.Owner ? g.ActivePoint.Owner.name : "", step = g.View.DisplayedStep, phase=g.ActivePoint.phase,
            caption = g.View.Caption, alpha = alpha, compact = g.View.Compact, playerOverlap = po, deviceOverlap = device, textFits = g.View.TextFits };
        string picture = flow.Stage + "-" + g.ActiveId + "-" + row.phase + "-" + row.step;
        if (Time.time - changed > .55f && Time.unscaledTime-lastCapture > .6f && (!captured || lastStep != row.step || po || device) && pictures.Add(picture))
        {
            row.image = "chapter-" + flow.Stage.ToString("00") + "-" + (++serial).ToString("00") + "-" + g.ActiveId + ".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, row.image));
            captured = true; lastStep = row.step; lastCapture = Time.unscaledTime;
        }
        File.AppendAllText(Path.Combine(Folder, "observations.jsonl"), JsonUtility.ToJson(row) + "\n");
    }
}
#endif
