#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only render-frame measurements, excluded from the player build.</summary>
[DefaultExecutionOrder(10000)]
public class MotionReviewProbe : MonoBehaviour
{
    public static MotionReviewProbe Instance;
    public readonly List<float[]> Frames = new List<float[]>();
    Transform backdrop;
    public static void Begin()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = new GameObject("Motion review probe").AddComponent<MotionReviewProbe>();
    }
    void LateUpdate()
    {
        var p = GameFlow.Instance ? GameFlow.Instance.Player : null;
        if (!p || !Camera.main || GameFlow.InputBlocked) return;
        if (!backdrop) { var go = GameObject.Find("NewArt Backdrop"); if (go) backdrop = go.transform; }
        var c = Camera.main.transform.position;
        var g = EnvironmentalGuidance.Instance;
        Frames.Add(new[] { Time.unscaledTime, Time.unscaledDeltaTime, c.x, c.y, p.transform.position.x,
            p.transform.position.y, p.Body.position.x, p.Body.position.y,
            backdrop ? backdrop.position.x-c.x : 0, backdrop ? backdrop.position.y-c.y : 0,
            g && g.View ? g.View.GetComponent<CanvasGroup>().alpha : 0 });
    }
}
#endif
