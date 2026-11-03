#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Editor-only test input. Uses the production controller and real fixed-step physics.</summary>
public class CampaignPilot : MonoBehaviour
{
    public static string Last = "Ready";
    public static string WalkTo(float x, float timeout = 45)
    {
        var p = GameFlow.Instance.Player;
        var pilot = p.GetComponent<CampaignPilot>();
        if (!pilot)
            pilot = p.gameObject.AddComponent<CampaignPilot>();
        pilot.StopAllCoroutines();
        pilot.StartCoroutine(pilot.Navigate(p, x, timeout));
        return "Walking to " + x;
    }
    IEnumerator Navigate(PlayerController p, float x, float timeout)
    {
        p.TestInputActive = true;
        GameFlow.Instance.Resume();
        float t = 0, next = 0;
        var lines = new List<string>();
        while (t < timeout && Mathf.Abs(p.transform.position.x - x) > .2f && GameFlow.Instance.Screen == FlowScreen.Playing)
        {
            float axis = Mathf.Sign(x - p.transform.position.x);
            p.TestHorizontal = axis;
            var b = p.GetComponent<Collider2D>().bounds;
            Vector2 ahead = new Vector2(b.center.x + axis * (b.extents.x + .32f), b.min.y + .1f);
            bool floor = Physics2D.Raycast(ahead, Vector2.down, .35f, p.groundLayer);
            bool wall = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + .25f), Vector2.right * axis, b.extents.x + .35f, p.groundLayer);
            p.TestJumpHeld = true;
            if (p.Grounded && (!floor || wall))
                p.TestJumpPressed = true;
            if (p.HasNearbyFood)
                p.SendMessage("Collect");
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
            if (t >= next)
            {
                lines.Add($"t={t:F2} position={p.transform.position} weight={p.Weight} grounded={p.Grounded}");
                next += .5f;
            }
        }
        p.TestHorizontal = 0;
        p.TestJumpHeld = false;
        Last = string.Join("\n", lines) + "\nScreen=" + GameFlow.Instance.Screen;
        Directory.CreateDirectory(".local/review");
        File.AppendAllText(".local/review/playthrough.log", Last + "\n");
        if (GameFlow.Instance.Screen == FlowScreen.Playing)
            GameFlow.Instance.SetScreen(FlowScreen.Paused);
    }
    public static string Drive(float axis, float seconds, bool jump = false)
    {
        var p = GameFlow.Instance.Player;
        var pilot = p.GetComponent<CampaignPilot>();
        if (!pilot)
            pilot = p.gameObject.AddComponent<CampaignPilot>();
        pilot.StopAllCoroutines();
        pilot.StartCoroutine(pilot.Segment(p, axis, seconds, jump));
        return "Input segment started";
    }
    IEnumerator Segment(PlayerController p, float axis, float seconds, bool jump)
    {
        p.TestInputActive = true;
        p.TestHorizontal = axis;
        p.TestJumpPressed = jump;
        p.TestJumpHeld = jump;
        GameFlow.Instance.Resume();
        var lines = new List<string>();
        float elapsed = 0, next = 0;
        while (elapsed < seconds && GameFlow.Instance.Screen == FlowScreen.Playing)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            if (elapsed >= next)
            {
                lines.Add($"t={elapsed:F2} position={p.transform.position} velocity={p.Body.linearVelocity} weight={p.Weight} grounded={p.Grounded}");
                next += .25f;
            }
        }
        p.TestHorizontal = 0;
        p.TestJumpHeld = false;
        Last = string.Join("\n", lines) + "\nScreen=" + GameFlow.Instance.Screen;
        Directory.CreateDirectory(".local/review");
        File.AppendAllText(".local/review/playthrough.log", Last + "\n");
        if (GameFlow.Instance.Screen == FlowScreen.Playing)
            GameFlow.Instance.SetScreen(FlowScreen.Paused);
    }
}
#endif
