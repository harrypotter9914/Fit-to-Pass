#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Test driver: production movement, jump, collect and consume; never teleports or creates food.</summary>
public class RegionalRoutePilot : MonoBehaviour
{
    public static string Status = "IDLE";
    public static int StepIndex;
    readonly List<string> trace = new List<string>();
    public static void Begin(string plan, bool bypassGuidance = true)
    {
        var p = GameFlow.Instance.Player;
        var pilot = p.GetComponent<RegionalRoutePilot>() ?? p.gameObject.AddComponent<RegionalRoutePilot>();
        pilot.StopAllCoroutines(); pilot.StartCoroutine(pilot.Run(plan, bypassGuidance));
    }
    IEnumerator Run(string plan, bool bypassGuidance)
    {
        var p = GameFlow.Instance.Player; var flow = GameFlow.Instance;
        p.TestInputActive = true; TutorialDirector.TestBypass = bypassGuidance; flow.Resume();
        Status = "RUNNING"; StepIndex = 0; trace.Clear();
        foreach (string line in plan.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] a = line.Trim().Split(',');
            string mode = a[0];
            float x = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
            float floor = float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
            int size = int.Parse(a[3]);
            float duration = a.Length > 4 ? float.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture) : 0;
            float time = 0, held = 0, nextJump = 0, nextFood = 0;
            bool passed = false;
            while (time < 80 && flow.Screen == FlowScreen.Playing)
            {
                var b = p.GetComponent<Collider2D>().bounds;
                float dx = x - p.transform.position.x;
                bool inAirflow = false; foreach (var field in Object.FindObjectsByType<VectorAirwell>()) if (field.Occupied) inAirflow = true;
                bool at = Mathf.Abs(dx) < (mode == "rail" ? .65f : .35f) && Mathf.Abs(b.min.y - floor) < (mode == "free" ? .4f : .8f) && (p.Grounded || inAirflow);
                if (mode == "spring") at = Mathf.Abs(dx) < .8f && p.Body.linearVelocity.y > 15;
                if (p.HasNearbyFood) p.SendMessage("Collect");
                int requested = mode == "rail" && Mathf.Abs(dx) > .65f ? (dx > 0 ? 0 : 2) : size;
                if (mode == "free")
                {
                    float velocity=p.Body.linearVelocity.y;
                    int presses=p.Size==2?Mathf.CeilToInt(Mathf.Max(0,p.Weight-11)/2f):p.Size==0?Mathf.CeilToInt(Mathf.Max(0,6-p.Weight)/2f):0;
                    float shiftDelay=Mathf.Max(0,presses-1)*.18f+.02f;
                    float braking=Mathf.Abs(velocity)*shiftDelay+velocity*velocity/60;
                    float band=p.Size==size?.35f:.15f;
                    requested=Mathf.Abs(dx)>1.6f?0:b.min.y>floor+band+(velocity<0?braking:0)?2:b.min.y<floor-band-(velocity>0?braking:0)?0:size;
                    at &= Mathf.Abs(velocity)<.35f;
                }
                if (requested >= 0 && time >= nextFood && p.Size != requested)
                {
                    p.Consume(p.Size < requested); nextFood = time + .18f;
                }
                p.TestHorizontal = Mathf.Abs(dx) > .25f ? Mathf.Sign(dx) : 0;
                if (mode == "rail") p.TestHorizontal = 0;
                p.TestJumpHeld = true;
                if (mode != "rail" && p.Grounded && Mathf.Abs(dx) > .4f && time > nextJump)
                {
                    float dir = Mathf.Sign(dx);
                    var ahead = new Vector2(b.center.x + dir * (b.extents.x + .35f), b.min.y + .12f);
                    bool ground = Physics2D.Raycast(ahead, Vector2.down, .45f, p.groundLayer);
                    bool wall = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + .08f), Vector2.right * dir, b.extents.x + .45f, p.groundLayer);
                    bool climb = floor > b.min.y + 1 && Mathf.Abs(dx) < 24 && mode == "jump";
                    if (!ground || wall || climb) { p.TestJumpPressed = true; nextJump = time + .4f; }
                }
                if (at && (size < 0 || p.Size == size))
                {
                    p.TestHorizontal = 0; held += Time.fixedDeltaTime;
                    if (mode == "echo") foreach (var e in Object.FindObjectsByType<MassEcho>()) e.Record();
                    bool deviceReady=true;
                    if(mode=="prism")
                    {
                        var prism=Object.FindObjectsByType<MassPrism>();
                        deviceReady=System.Array.Exists(prism,q=>Mathf.Abs(q.transform.position.x-x)<.5f && q.Charge[size]>=1);
                    }
                    if (held >= duration && deviceReady) { passed = true; break; }
                }
                else held = 0;
                yield return new WaitForFixedUpdate(); time += Time.fixedDeltaTime;
            }
            trace.Add(StepIndex + ": " + line + " => " + passed + " pos=" + p.transform.position + " w=" + p.Weight + " food=" + p.JunkCount + "/" + p.HealthyCount + " time=" + time + " velocity=" + p.Body.linearVelocity + " input=" + p.HorizontalInput);
            if (!passed && flow.Screen != FlowScreen.Complete) { Status = "FAILED " + StepIndex + " " + line; break; }
            StepIndex++;
        }
        p.TestHorizontal = 0; p.TestJumpHeld = false;
        if (Status == "RUNNING") Status = flow.Screen == FlowScreen.Complete ? "COMPLETE" : "PLAN FINISHED";
        var circuit = Object.FindAnyObjectByType<RouteCircuit>();
        trace.Add("FINAL " + Status + " screen=" + flow.Screen + " objectives=" + (circuit ? circuit.NextObjective : "original map"));
        Directory.CreateDirectory(".local/review");
        File.WriteAllLines(".local/review/regional-route-" + flow.Stage + ".log", trace);
        if (flow.Screen == FlowScreen.Playing) flow.SetScreen(FlowScreen.Paused);
    }
}
#endif
