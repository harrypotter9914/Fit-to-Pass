using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>One local, world-anchored lesson at a time. Never changes time, input or collision.</summary>
[DefaultExecutionOrder(300)]
public class EnvironmentalGuidance : MonoBehaviour
{
    public static EnvironmentalGuidance Instance { get; private set; }
    public string ActiveId => active ? active.id : "";
    public TutorialPoint ActivePoint => active;
    public WorldGuideView View { get; private set; }
    public Rect Placement { get; private set; }
    public bool OnWall { get; private set; }
    public int Stage { get; private set; }
    public int CompletedLessons { get; private set; }
    PlayerController player;
    TutorialPoint active;
    float readyAt, shownAt, leavingAt = -1;
    Vector3 start;
    int jumps, collected, gain, fresh;
    bool compact, familiar, usedWind, usedLock, recorded;
    Vector3 deviceStart;
    float completedAt, visibleFor;
    SpriteRenderer[] props;
    Renderer[] signs;
    Component[] sources;
    TutorialPoint[] points;
    SpriteRenderer[] subjectArt;
    float opacity, blockedUntil;
    readonly HashSet<TutorialPoint> dismissed = new HashSet<TutorialPoint>();
    string request;
    readonly HashSet<string> presented = new HashSet<string>();
    readonly Dictionary<TutorialPoint,float> cooldown = new Dictionary<TutorialPoint,float>();
    public static void BeginStage(int stage, PlayerController p, bool retry = false)
    {
        if (Instance) Destroy(Instance.gameObject);
        var go = new GameObject("Environmental teaching / chapter " + stage);
        var guide = go.AddComponent<EnvironmentalGuidance>();
        Instance = guide; guide.Stage = stage; guide.player = p; guide.readyAt = Time.unscaledTime + .2f;
        TutorialDirector.Discover("movement");
        if (stage == 1 && !TutorialDirector.Seen("movement")) TutorialDirector.Point("movement", p.transform.position, 6);
        if (!retry && (stage != 1 || TutorialDirector.Seen("movement")))
        {
            string id = CampaignMap.Region(stage) == 1 ? "heat" : CampaignMap.Region(stage) == 2 ? "cold" : "ambient";
            TutorialDirector.Point(id, p.transform.position, 7);
            if (id != "ambient") TutorialDirector.Discover(id);
        }
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public void Request(string id) { request = id; }
    static bool Basic(string id) => id == "movement" || id == "jump" || id == "food" || id == "metabolism" || id == "score" || id == "duct" || id == "checkpoint";
    static string TopicKey(TutorialPoint p) => p.id+"/"+p.phase+(p.Owner is BalanceBridge b?"/"+b.riderSide:"")+(p.Owner is RailShuttle r && r.requireReturn?"/return":"");
    bool Eligible(TutorialPoint p)
    {
        if (!p || p.UsedThisVisit || dismissed.Contains(p) || cooldown.TryGetValue(p,out float until) && Time.time < until) return false;
        if (FirstEncounterGuide.Manages(p.id)) return false;
        if ((p.id=="heat" || p.id=="cold" || p.id=="ambient") && TutorialDirector.Seen(p.id)) return false;
        if (Basic(p.id) && TutorialDirector.Seen(p.id)) return false;
        if (TutorialDirector.Seen(p.id) && presented.Contains(TopicKey(p))) return false;
        if (p.Owner is RailShuttle rail)
        {
            if (rail.Docked) return false;
            if (p.phase == 1 && rail.requireReturn && !rail.ReturnLegStarted) return false;
            if (p.phase == 2 && rail.ReturnLegStarted) return false;
        }
        Vector3 d = p.transform.position - player.GetComponent<Collider2D>().bounds.center;
        return Mathf.Abs(d.y) < 4.1f && Mathf.Abs(d.x) < p.radius;
    }
    int Priority(TutorialPoint p)
    {
        if (p.Owner is RailShuttle && p.phase > 0) return 125;
        if (p.Owner is RailShuttle variant && variant.requireReturn || p.Owner is BalanceBridge reverse && reverse.riderSide>0) return 118;
        if (p.id == "heat" || p.id == "cold" || p.id == "ambient") return 120;
        if (p.id == "movement") return 110;
        if (p.id == "jump") return 100;
        if (p.id == "food") return 90;
        if (p.id == "metabolism") return 65;
        if (p.id == "checkpoint" || p.id == "score") return 20;
        return TutorialDirector.Seen(p.id) ? 80 : 115;
    }
    void LateUpdate()
    {
        if (!player || !GameFlow.Instance) return;
        if (GameFlow.InputBlocked || GameFlow.Instance.Loading) { if (View) View.SetOpacity(0); return; }
#if UNITY_EDITOR
        if (TutorialDirector.TestBypass) { if (View) View.SetOpacity(0); return; }
#endif
        if (Time.unscaledTime < readyAt) return;
        if (points == null) points = UnityEngine.Object.FindObjectsByType<TutorialPoint>();
        if (subjectArt == null) subjectArt = player.GetComponentsInChildren<SpriteRenderer>();
        dismissed.RemoveWhere(p=>!p || Mathf.Abs(player.transform.position.x-p.transform.position.x)>p.radius+4 || Mathf.Abs(player.transform.position.y-p.transform.position.y)>10);
        if (sources == null) sources = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Where(m=>Kind(m)!=null).Cast<Component>().ToArray();
        if (signs == null) signs = UnityEngine.Object.FindObjectsByType<TextMesh>().Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray();
        if (props == null) props = UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r => r.sortingOrder >= 4 && !r.GetComponentInParent<PlayerController>()).ToArray();
        if (active)
        {
            float age = Time.time - shownAt;
            // A previous mechanism must not conceal the next unfamiliar action.
            bool climate=active.id=="heat" || active.id=="cold" || active.id=="ambient";
            TutorialPoint urgent = null;
            foreach (var candidate in points)
                if (candidate!=active && Eligible(candidate) && (Priority(candidate)>Priority(active) || climate && age>2.2f && !TutorialDirector.Seen(candidate.id) && candidate.Owner) && (!urgent || Priority(candidate)>Priority(urgent))) urgent=candidate;
            if (urgent && age > 2.2f && leavingAt<0)
            {
                leavingAt=Time.time;
            }
            bool completed = Completed(active, age);
            bool clear=ActionAreaClear();if(clear)visibleFor+=Time.deltaTime;
            if (completed && visibleFor > .2f && !familiar && !recorded)
            {
                TutorialDirector.Learned(active.id); recorded = true; completedAt = Time.time; CompletedLessons++;
                if (Basic(active.id)) View.SetSuccess();
                if (active.id == "food") TutorialDirector.Discover("mass");
            }
            var displacement = player.transform.position - active.transform.position;
            bool away = Mathf.Abs(displacement.x) > active.radius + 3 || Mathf.Abs(displacement.y) > 9;
            bool timed = (familiar && age > 4f) || ((active.id == "heat" || active.id == "cold" || active.id == "ambient") && age > 4.8f) || (active.id == "metabolism" && age > 7);
            if (leavingAt>=0 || (recorded && Time.time-completedAt > 1.8f) || away || timed || !InSafeView(Placement, Camera.main))
            {
                if (leavingAt < 0) leavingAt = Time.time;
                Fade(0);
                if (Time.time - leavingAt > .5f)
                {
                    active.UsedThisVisit = completed || familiar || timed;
                    if (active.UsedThisVisit && active.Owner)
                        foreach (var other in UnityEngine.Object.FindObjectsByType<TutorialPoint>())
                            if (other.id == active.id && other.phase == active.phase && Resolve(other) == active.Owner) other.UsedThisVisit = true;
                    cooldown[active] = Time.time + 8;
                    dismissed.Add(active);
                    Destroy(View.gameObject); active = null; View = null; leavingAt = -1;
                }
            }
            else
            {
                // Fade before the actor reaches the paint, then wait for a clear
                // area instead of flickering on every contact/edge threshold.
                if (!clear || Approaching()) blockedUntil=Time.time+.65f;
                Fade(clear && Time.time>=blockedUntil ? 1 : 0);
                View.Animate(active.id, Step(active,age), active.Owner, player, age);
            }
            return;
        }
        var next = points.Where(Eligible).OrderByDescending(Priority).ThenBy(p => (p.transform.position-player.transform.position).sqrMagnitude).FirstOrDefault();
        if (!string.IsNullOrEmpty(request)) { next = points.Where(p=>p.id==request && Eligible(p)).FirstOrDefault() ?? next; request = null; }
        if (next) Present(next);
    }
    void Fade(float target)
    {
        opacity=Mathf.MoveTowards(opacity,target,Time.deltaTime*(target>opacity?2.5f:6.25f));
        if (!ActionAreaClear()) opacity=0;
        View.SetOpacity(opacity);
    }
    bool Approaching()
    {
        var body=player.GetComponent<Collider2D>().bounds;
        Vector3 motion=(Vector3)(player.Body.linearVelocity*.22f);
        body.Encapsulate(new Bounds(body.center+motion,body.size));
        return body.Intersects(new Bounds(Placement.center,new Vector3(Placement.width+.3f,Placement.height+.3f,2)));
    }
    static string Kind(Component m)
    {
        if(!m)return null;
        return m is MassGate?"gate":m is KineticCapacitor?"capacitor":m is MassEcho?"echo":m is RailShuttle?"shuttle":m is MassPrism?"prism":m is BodyRelay?"relay":m is MassLockStation?"masslock":m is VectorAirwell?(((VectorAirwell)m).tiered?"airwell":"freeflight"):m is ButtonController?"pressure":m is PlatformAndRockController?"counterweight":m is ElevatorController?"elevator":m is SpringController?"spring":m is BalanceBridge?"bridge":m is CheckpointBeacon?"checkpoint":null;
    }
    Component Resolve(TutorialPoint point)
    {
        if(point.Owner)return point.Owner;
        point.Owner=(sources ?? Array.Empty<Component>()).Where(m=>m && Kind(m)==point.id && Mathf.Abs(m.transform.position.y-point.transform.position.y)<2.5f && Vector2.Distance(m.transform.position,point.transform.position)<8).OrderBy(m=>(m.transform.position-point.transform.position).sqrMagnitude).FirstOrDefault();
        return point.Owner;
    }
    void Present(TutorialPoint point)
    {
        Resolve(point);
        familiar = TutorialDirector.Seen(point.id) && point.id != "heat" && point.id != "cold" && point.id != "ambient" && !(point.Owner is RailShuttle variant && variant.requireReturn) && !(point.Owner is BalanceBridge reverse && reverse.riderSide>0);
        compact = familiar;
        if (point.id == "heat" || point.id == "cold") compact = CampaignMap.Local(Stage) != 1;
        if (point.id == "ambient") compact = true;
        if (!TryPlace(point, compact, out Rect rect, out bool wall))
        {
            compact = true;
            if (!TryPlace(point, true, out rect, out wall)) { cooldown[point] = Time.time + .8f; return; }
        }
        active = point; Placement = rect; OnWall = wall; shownAt = Time.time; visibleFor=0; opacity=0; blockedUntil=0;
        start = player.transform.position; jumps = player.JumpCount; collected = player.CollectedFoods; gain = player.ConsumedEnergy; fresh = player.ConsumedFresh; usedWind = usedLock = false; recorded = false; completedAt = -1; deviceStart = point.Owner ? point.Owner.transform.position : point.transform.position;
        TutorialDirector.Discover(point.id);
        presented.Add(TopicKey(point));
        if (point.id == "duct") TutorialDirector.Discover("food");
        var go = new GameObject("World chalk / " + point.id, typeof(RectTransform)); go.transform.SetParent(transform, false);
        View = go.AddComponent<WorldGuideView>(); View.Phase = point.phase; View.Build(rect, wall, compact, Stage);
        View.SetOpacity(0);
        View.Animate(point.id, 0, point.Owner, player, 0);
    }
    public bool ActionAreaClear()
    {
        var rect=Placement;var b=new Bounds(rect.center,new Vector3(rect.width+.12f,rect.height+.12f,2));
        var body=player.GetComponent<Collider2D>().bounds;body.Expand(.15f);
        if (b.Intersects(body)) return false;
        foreach (var r in subjectArt ?? Array.Empty<SpriteRenderer>()) if(r && r.enabled && r.gameObject.activeInHierarchy && b.Intersects(r.bounds)) return false;
        foreach (var r in props ?? Array.Empty<SpriteRenderer>()) if(r && r.enabled && r.gameObject.activeInHierarchy && b.Intersects(r.bounds)) return false;
        var flow=GameFlow.Instance;var camera=Camera.main;
        if(flow && camera && flow.NoticeUntil>Time.unscaledTime)
        {
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            var lo=camera.WorldToScreenPoint(new Vector3(rect.xMin,rect.yMin));var hi=camera.WorldToScreenPoint(new Vector3(rect.xMax,rect.yMax));
            var mark=new Rect(lo.x,Screen.height-hi.y,hi.x-lo.x,hi.y-lo.y);
            var toast=new Rect((Screen.width-1600*scale)*.5f+490*scale,(Screen.height-900*scale)*.5f+130*scale,620*scale,60*scale);
            if(mark.Overlaps(toast))return false;
        }
        return true;
    }
    int Step(TutorialPoint p, float age)
    {
        if (p.Owner is MassGate gate) return gate.Open ? 2 : player.Size == gate.requiredSize ? 1 : 0;
        if (p.Owner is SpringController) return player.Body.linearVelocity.y>1 ? 1 : player.Size==0 ? 2 : 0;
        if (p.Owner is BodyRelay relay) return relay.Charged ? 2 : player.Size == relay.requiredSize ? 1 : 0;
        if (p.Owner is KineticCapacitor core) return core.Powered ? 2 : player.Size == 2 ? 1 : 0;
        if (p.Owner is MassEcho echo) return echo.Powered ? 2 : player.Size == 2 ? 1 : 0;
        if (p.Owner is MassLockStation) { var hold=player.GetComponent<MassLock>(); return hold && hold.Locked ? (hold.PendingWeight != player.Weight ? 2 : 1) : 0; }
        if (p.Owner is MassPrism prism) return prism.Complete ? 2 : player.Size;
        if (p.Owner is VectorAirwell) return player.Size;
        if (p.Owner is ElevatorController) return player.Size;
        if (p.Owner is BalanceBridge balance) return (player.transform.position.x-balance.transform.position.x)*balance.riderSide>0 ? 1 : 0;
        if (p.Owner is CheckpointBeacon beacon) return beacon.Activated ? 2 : 0;
        if (p.Owner is ButtonController button && button.platformController)
        {
            var lift=button.platformController;
            if(Mathf.Abs(player.transform.position.x-lift.transform.position.x)<1.8f && player.transform.position.y>lift.transform.position.y)return 2;
            return lift.transform.position.y<lift.upPosition.y-.2f ? 1 : 0;
        }
        if (p.Owner is PlatformAndRockController rock) return rock.rockTransform.position.y>rock.rockDownPosition.y+.7f ? (Mathf.Abs(player.transform.position.x-rock.transform.position.x)<2 ? 1 : 2) : 0;
        if (p.Owner is RailShuttle rail) return rail.Docked ? 2 : p.phase==1 || rail.requireReturn && p.phase==2 && rail.TurnaroundReached ? 1 : 0;
        return 0;
    }
    bool Completed(TutorialPoint p, float age)
    {
        switch (p.id)
        {
            case "movement": return Mathf.Abs(player.transform.position.x-start.x)>2.5f;
            case "jump": return player.JumpCount > jumps;
            case "food": return player.CollectedFoods > collected;
            case "duct": return player.Size == 0 && (player.ConsumedFresh > fresh || Mathf.Abs(player.transform.position.x-p.transform.position.x)<2.5f);
            case "metabolism": return visibleFor > 1.6f;
            case "heat": case "cold": case "ambient": return age > 4;
            case "score": return age > 4;
        }
        if (p.Owner is MassGate gate) return gate.Open;
        if (p.Owner is BodyRelay relay) return relay.Charged;
        if (p.Owner is KineticCapacitor core) return core.Powered;
        if (p.Owner is MassEcho echo) return echo.Powered;
        if (p.Owner is RailShuttle rail) return rail.Docked;
        if (p.Owner is MassPrism prism) return prism.Complete;
        if (p.Owner is MassLockStation) { var hold=player.GetComponent<MassLock>(); usedLock |= hold && hold.Locked; return usedLock && !(hold && hold.Locked); }
        if (p.Owner is VectorAirwell air) { usedWind |= air.Occupied; return usedWind && (air.Occupied && (Mathf.Abs(player.transform.position.y-start.y)>1.5f || player.ConsumedEnergy>gain || player.ConsumedFresh>fresh) || !air.Occupied && player.Grounded); }
        if (p.Owner is CheckpointBeacon beacon) return beacon.Activated;
        if (p.Owner is SpringController) return player.Body.linearVelocity.y>13 && player.transform.position.y>p.transform.position.y;
        if (p.Owner is ElevatorController lift) return Mathf.Abs(lift.transform.position.y-deviceStart.y)>.4f && player.Size==1 && player.Grounded && Mathf.Abs(player.transform.position.x-lift.transform.position.x)<1.7f && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-lift.GetComponent<Collider2D>().bounds.max.y)<.3f;
        if (p.Owner is BalanceBridge bridge) return player.Size==1 && player.Grounded && Mathf.Abs(bridge.GetComponent<Rigidbody2D>().rotation)<3 && Mathf.Abs(player.transform.position.x-bridge.transform.position.x)<4;
        if (p.Owner is ButtonController plate && plate.platformController)
        { var pressureLift = plate.platformController; return pressureLift.transform.position.y>pressureLift.downPosition.y+.3f && player.Grounded && Mathf.Abs(player.transform.position.x-pressureLift.transform.position.x)<1.7f && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-pressureLift.GetComponent<Collider2D>().bounds.max.y)<.3f; }
        if (p.Owner is PlatformAndRockController rock) return player.transform.position.x>rock.rockTransform.GetComponent<Collider2D>().bounds.max.x+.3f;
        return age > 6;
    }
    static bool Terrain(Collider2D c) => c.GetComponent<Tilemap>() || c.name.StartsWith("RoomSurface");
    public bool TryPlace(TutorialPoint point, bool small, out Rect placement, out bool onWall)
    {
        float width = small ? 4.48f : 5.6f, height = small ? 1.52f : 2.6f;
        Vector3 basePoint = player.GetComponent<Collider2D>().bounds.center;
        var camera = Camera.main; float best = float.PositiveInfinity;
        placement = default; onWall = false;
        foreach (float dy in new[] { 2.7f, 3.7f, 1.8f, 1.4f, 2.2f, 3.2f, 1f, .4f, -.8f, 4.6f })
            foreach (float dx in new[] { 2.8f, -2.8f, 4f, -4f, 0f, 1.4f, -1.4f, 5.2f, -5.2f, 6.2f, -6.2f })
            {
                Vector2 center = (Vector2)basePoint + new Vector2(dx,dy);
                var rect = new Rect(center-new Vector2(width,height)*.5f,new Vector2(width,height));
                if (!InSafeView(rect,camera)) continue;
                var bounds = new Bounds(center,new Vector3(width+.2f,height+.2f,2));
                var body = player.GetComponent<Collider2D>().bounds; body.Expand(.65f);
                if (bounds.Intersects(body)) continue;
                var hits = Physics2D.OverlapBoxAll(center,new Vector2(width+.2f,height+.2f),0,1<<6);
                if (hits.Any(c=>!c.isTrigger && !Terrain(c))) continue;
                bool prop = false;
                foreach (var r in props ?? Array.Empty<SpriteRenderer>())
                    if (r && r.enabled && bounds.Intersects(r.bounds)) { prop=true;break; }
                if (!prop) foreach (var r in signs ?? Array.Empty<Renderer>()) if (r && bounds.Intersects(r.bounds)) { prop = true; break; }
                if (prop) continue;
                bool wall = false;
                if (hits.Any(c=>!c.isTrigger && Terrain(c)))
                {
                    int samples = 0;
                    for(int x=0;x<3;x++)for(int y=0;y<3;y++)
                    { var at = center+new Vector2((x-1)*width*.43f,(y-1)*height*.4f); if(Physics2D.OverlapPointAll(at,1<<6).Any(c=>!c.isTrigger && Terrain(c))) samples++; }
                    if (samples < 9) continue;
                    wall = true;
                }
                float score = (wall ? 40 : 0) + Mathf.Abs(dx)*.35f+Mathf.Abs(dy-2.7f)*2;
                if (score < best) { best=score;placement=rect;onWall=wall; }
            }
        return best < float.PositiveInfinity;
    }
    static bool InSafeView(Rect rect, Camera camera)
    {
        if (!camera) return false;
        var lo=camera.WorldToViewportPoint(new Vector3(rect.xMin,rect.yMin));
        var hi=camera.WorldToViewportPoint(new Vector3(rect.xMax,rect.yMax));
        return lo.x>.055f && hi.x<.945f && lo.y>.21f && hi.y<.84f;
    }
}
