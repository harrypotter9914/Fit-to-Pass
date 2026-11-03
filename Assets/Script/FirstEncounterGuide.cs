using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>First-use explanations are acknowledged separately from action completion.</summary>
[DefaultExecutionOrder(350)]
public class FirstEncounterGuide : MonoBehaviour
{
    public class Lesson
    {
        public string Id, Title, Purpose;
        public string[] Steps, Keys;
        public Lesson(string id,string title,string purpose,string steps,string keys="")
        { Id=id;Title=title;Purpose=purpose;Steps=steps.Split('|');Keys=keys.Split('|'); }
    }
    public class Entry
    {
        public Lesson Lesson;
        public Component Owner;
        public int Chapter;
        public string OwnerName, Relation;
    }
    public static FirstEncounterGuide Instance { get; private set; }
    public const float MinimumReadTime=4.5f;
    public Entry Local { get; private set; }
    public Entry Global { get; private set; }
    public float ReadSeconds { get; private set; }
    public float GlobalSeconds { get; private set; }
    public bool LocalVisible { get; private set; }
    public Rect Panel { get; private set; }
    public int Page { get; private set; }
    public bool Compact => Panel.width<500;
    public bool CanAcknowledge => LocalVisible && ReadSeconds>=MinimumReadTime && (!Compact || Page==2);
    public int PendingCount => pending.Count;
    readonly Queue<Entry> pending=new Queue<Entry>();
    readonly Queue<Entry> globalQueue=new Queue<Entry>();
    readonly HashSet<string> queued=new HashSet<string>();
    PlayerController player;
    TutorialPoint[] points;
    Component[] sources;
    SpriteRenderer[] art;
    int chapter;
    float readyAt, nextScan;
    bool globalsChecked;
    public static readonly Dictionary<string,Lesson> Lessons=new Dictionary<string,Lesson>
    {
        {"chapter_circuit",new Lesson("chapter_circuit","RESTORE THE EXIT CIRCUIT","The exit counts completed route links. It does not count floors or food.",
            "MATCH EACH R BADGE|WATCH EXIT LINKS FILL|ALL LINKS OPEN THE EXIT","1 / 2||")},
        {"optics_circuit",new Lesson("optics_circuit","THREE BEAMS. ONE POWER CIRCUIT.","A prism uses your body size to light three receivers. All three power its linked locks.",
            "S TOP / M MIDDLE / L LOW|FILL ALL THREE RECEIVERS|POWER THE MARKED LOCKS","1 / 2||")},
        {"pressure",new Lesson("pressure","CALL A LIFT FROM ABOVE","A heavy body presses this plate to bring its lift down. Board the lift to ride back up.",
            "BECOME HEAVY|JUMP ONTO THE PLATE|BOARD THE WAITING LIFT","1|SPACE|A / D")},
        {"counterweight",new Lesson("counterweight","OPEN THE PATH. THEN RACE.","Your weight raises the blocking stone. Leaving the board starts closing the path again.",
            "HEAVY RAISES THE STONE|MEDIUM HOLDS THE HEIGHT|SMALL RUNS THROUGH","1|1 / 2|2")},
        {"elevator",new Lesson("elevator","YOUR BODY SETS THE HEIGHT","Ride this platform between floors. Change size aboard to reach a landing or hold its height.",
            "SMALL: RISE|MEDIUM: HOLD|HEAVY: DESCEND","2|1 / 2|1")},
        {"spring",new Lesson("spring","CHOOSE YOUR BOUNCE","Reach higher shelves with a small body. Plan the size you need when you land.",
            "SHRINK BEFORE LANDING|LAND TO BOUNCE HIGH|STEER ONTO THE SHELF","2|SPACE|A / D")},
        {"bridge",new Lesson("bridge","BALANCE OPPOSITE THE CRATE","The crate is a counterweight. Medium balances it; lighter or heavier bodies tilt the deck.",
            "STAND OPPOSITE THE CRATE|MATCH WITH MEDIUM|CROSS THE LEVEL DECK","A / D|1 / 2|SPACE")},
        {"bridge_reverse",new Lesson("bridge_reverse","THE COUNTERWEIGHT CHANGED SIDES","This bridge reverses the arrangement. Cross the crate and balance from the other side.",
            "CLEAR THE CRATE|BALANCE FROM THE RIGHT|LINK THE FAR LANDING","SPACE|1 / 2|A / D")},
        {"checkpoint",new Lesson("checkpoint","A SAFE PLACE TO TRY AGAIN","Touch the blue beacon to save your place, body and food for retries in this chapter run.",
            "TOUCH THE BLUE BEACON|YOUR RETRY IS RECORDED|TRY AGAIN FROM HERE","A / D||R")},
        {"gate",new Lesson("gate","MATCH THE SCANNER BADGE","This door checks one body size. Match its S, M or L badge and stay still for the scan.",
            "READ THE LIT BODY BADGE|MATCH IT AND STAND STILL|GREEN: WALK THROUGH","|1 / 2|A / D")},
        {"capacitor",new Lesson("capacitor","TURN A HEAVY LANDING INTO A BRIDGE","A heavy impact powers a temporary crossing. Standing on the core alone does not charge it.",
            "GET HEAVY|HOLD JUMP. LAND HEAVY|SHRINK AND CROSS","1|SPACE|2")},
        {"echo",new Lesson("echo","LEAVE WEIGHT TO HOLD A GATE OPEN","Your recorded heavy echo powers its marked gate while you become small enough to cross.",
            "STAND HERE WHILE HEAVY|PRESS E TO RECORD|SHRINK AND CROSS IN TIME","1|E|2")},
        {"shuttle",new Lesson("shuttle","MOVE THE CARRIAGE. STORE THE DOCK LINK.","Your body steers this carriage. A medium stop at the cyan dock is also an exit objective.",
            "SMALL RIGHT / HEAVY LEFT|MEDIUM: STOP ON CYAN|WAIT FOR DOCK LINK STORED","1 / 2|1 / 2|")},
        {"shuttle_return",new Lesson("shuttle_return","REACH THE FAR STOP. THEN RETURN.","This line needs a return journey. A first outward stop alone cannot store its dock link.",
            "SMALL: REACH THE FAR STOP|HEAVY: RETURN LEFT|MEDIUM: BRAKE ON CYAN","2|1|2")},
        {"prism",new Lesson("prism","LIGHT EVERY RECEIVER","Stand on the prism pad. Each body size routes light to a different receiver and its power link.",
            "SMALL: UPPER RECEIVER|MEDIUM: MIDDLE RECEIVER|HEAVY: LOWER RECEIVER","2|1 / 2|1")},
        {"relay",new Lesson("relay","THIS NODE IS PART OF THE EXIT LOCK","Match the node's body badge until it lights up. Its R number matches the exit checklist.",
            "READ THIS R NODE'S BADGE|HOLD THE MATCHING SIZE|ITS EXIT LINK STAYS LIT","|1 / 2|")},
        {"airwell",new Lesson("airwell","ONE WIND SHAFT. THREE ROUTES.","Body size selects a landing height. Change size inside the wind to reach another passage.",
            "SMALL: HIGH ROUTE|MEDIUM: MIDDLE ROUTE|HEAVY: LOW ROUTE","2|1 / 2|1")},
        {"freeflight",new Lesson("freeflight","RISE, HOLD OR DESCEND INTO A SIDE EXIT","This wind has no fixed landing tiers. Medium brakes at your current height inside the field.",
            "SMALL RISES / HEAVY FALLS|MEDIUM HOLDS YOUR HEIGHT|STEER INTO THE SIDE ROUTE","1 / 2|1 / 2|A / D")},
        {"masslock",new Lesson("masslock","KEEP ONE SHAPE WHILE PREPARING THE NEXT","Lock a useful shape so movement or rest cannot change it mid-crossing. Release your stored change later.",
            "E: HOLD YOUR CURRENT SHAPE|MOVE / EAT: STORE CHANGES|Q: APPLY THEM IN OPEN SPACE","E|1 / 2|Q")},
        {"linkeddoor",new Lesson("linkeddoor","THIS GATE NEEDS A REMOTE POWER LINK","Read the matching link code on the gate and its source. Your body alone cannot power this gate.",
            "READ THE LOCK'S LINK CODE|FOLLOW ITS SOURCE MARKER|RESTORE POWER THEN RETURN","||A / D")},
        {"exit",new Lesson("exit","FINISH THE CHECKLIST TO LEAVE","Route nodes, required docks and optical circuits feed the exit. The next missing item is named above.",
            "CHECK EXIT LINKS ABOVE|COMPLETE THE NAMED ITEM|RETURN WHEN THE EXIT IS READY","||A / D")}
    };
    public static string Key(string id)=>"Fit.Explained.v5."+id;
    public static bool Explained(string id)=>PlayerPrefs.GetInt(Key(id),0)!=0;
    public static bool Manages(string id)=>Lessons.ContainsKey(id) && id!="chapter_circuit" && id!="optics_circuit";
    public static void ClearProgress()
    {
        foreach(var id in Lessons.Keys)PlayerPrefs.DeleteKey(Key(id));
        if(Instance)Instance.ClearPending();
    }
    void Awake(){Instance=this;}
    void OnDestroy(){if(Instance==this)Instance=null;}
    public void BeginStage(int stage,PlayerController subject)
    {
        if(chapter!=stage)ClearPending();
        chapter=stage;player=subject;points=null;sources=null;art=null;
        readyAt=Time.unscaledTime+.3f;globalsChecked=false;nextScan=0;LocalVisible=false;
    }
    public void ClearPending()
    {
        pending.Clear();globalQueue.Clear();queued.Clear();Local=Global=null;LocalVisible=false;ReadSeconds=GlobalSeconds=0;Page=0;
    }
    static string Topic(TutorialPoint p)
        =>p.Owner is RailShuttle r && r.requireReturn?"shuttle_return":p.Owner is BalanceBridge b && b.riderSide>0?"bridge_reverse":p.id;
    Entry Make(string id,Component owner)
    {
        var entry=new Entry{Lesson=Lessons[id],Owner=owner,OwnerName=owner?owner.name:"",Chapter=chapter};
        entry.Relation=MechanismConnections.Relation(owner,player);return entry;
    }
    public void Request(string id,Component owner=null)
    {
        if(!Manages(id) || Explained(id) || !queued.Add(id))return;
        pending.Enqueue(Make(id,owner));
        TutorialDirector.Discover(id=="bridge_reverse"?"bridge":id=="shuttle_return"?"shuttle":id);
    }
    void GlobalRequest(string id)
    {
        if(!Explained(id) && queued.Add(id)){globalQueue.Enqueue(Make(id,null));TutorialDirector.Discover(id);}
    }
    public void Acknowledge()
    {
        if(!CanAcknowledge)return;
        PlayerPrefs.SetInt(Key(Local.Lesson.Id),1);PlayerPrefs.Save();
        queued.Remove(Local.Lesson.Id);Local=null;LocalVisible=false;ReadSeconds=0;Page=0;
    }
    public void Advance()
    {
        if(!LocalVisible)return;
        if(Compact && Page<2){Page++;return;}
        Acknowledge();
    }
    public void Previous(){if(Page>0)Page--;}
    public void AcknowledgeGlobal()
    {
        if(Global==null || GlobalSeconds<1.2f)return;
        PlayerPrefs.SetInt(Key(Global.Lesson.Id),1);PlayerPrefs.Save();queued.Remove(Global.Lesson.Id);
        Global=null;GlobalSeconds=0;GameFlow.Instance.SetScreen(FlowScreen.Playing);
    }
    void Update()
    {
        var flow=GameFlow.Instance;if(!flow)return;
        if(flow.Stage==0){if(Local!=null || Global!=null || pending.Count>0)ClearPending();return;}
#if UNITY_EDITOR
        if(TutorialDirector.TestBypass)return;
#endif
        if(flow.Screen==FlowScreen.Discovery)
        {
            GlobalSeconds+=Time.unscaledDeltaTime;
            if(Input.GetKeyDown(KeyCode.Return))AcknowledgeGlobal();
            return;
        }
        if(flow.Screen!=FlowScreen.Playing || flow.Loading || !player || Time.unscaledTime<readyAt)return;
        if(!globalsChecked)
        {
            globalsChecked=true;
            if(UnityEngine.Object.FindAnyObjectByType<RouteCircuit>())GlobalRequest("chapter_circuit");
            if(UnityEngine.Object.FindAnyObjectByType<MassPrism>())GlobalRequest("optics_circuit");
        }
        if(globalQueue.Count>0)
        {
            Global=globalQueue.Dequeue();GlobalSeconds=0;flow.SetScreen(FlowScreen.Discovery);return;
        }
        if(points==null)points=UnityEngine.Object.FindObjectsByType<TutorialPoint>();
        if(sources==null)sources=UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Where(m=>Kind(m)!=null).Cast<Component>().ToArray();
        if(Time.unscaledTime>=nextScan)
        {
            nextScan=Time.unscaledTime+.1f;
            foreach(var point in points.Where(p=>p && Manages(p.id)).OrderBy(p=>p.id=="checkpoint"?1:0).ThenBy(p=>(p.transform.position-player.transform.position).sqrMagnitude))
            {
                var d=point.transform.position-player.GetComponent<Collider2D>().bounds.center;
                if(Mathf.Abs(d.x)>point.radius || Mathf.Abs(d.y)>4.1f)continue;
                if(!point.Owner)point.Owner=sources.Where(s=>s && Kind(s)==point.id && Vector2.Distance(s.transform.position,point.transform.position)<9).OrderBy(s=>(s.transform.position-point.transform.position).sqrMagnitude).FirstOrDefault();
                if(!SameLanding(point.Owner))continue;
                Request(Topic(point),point.Owner);
            }
        }
        if(Local==null && pending.Count>0){Local=pending.Dequeue();ReadSeconds=0;Page=0;Panel=default;}
        if(Local==null)return;
        if(!Local.Owner && Local.Chapter==chapter && !string.IsNullOrEmpty(Local.OwnerName))
            Local.Owner=sources.FirstOrDefault(s=>s && s.name==Local.OwnerName);
        if(Local.Owner)Local.Relation=MechanismConnections.Relation(Local.Owner,player);
    }
    bool SameLanding(Component source)
    {
        // A tall body or jump beneath an upper floor is not an encounter with
        // that floor's pad. Wind volumes and pivoted mechanisms span heights.
        if(!source || source is VectorAirwell || source is BalanceBridge || source is PlatformAndRockController)return true;
        float floor=source.transform.position.y;
        var shape=source.GetComponent<Collider2D>();
        if(shape && (source is MassGate || source is SignalBarrier || source is LevelGoal || source is KineticCapacitor || source is ButtonController || source is RailShuttle || source is SpringController))floor=shape.bounds.min.y;
        return Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y-floor)<1.35f;
    }
    void LateUpdate()
    {
        var flow=GameFlow.Instance;
        if(!flow || flow.Screen!=FlowScreen.Playing || Local==null || !player){LocalVisible=false;return;}
#if UNITY_EDITOR
        if(TutorialDirector.TestBypass){LocalVisible=false;return;}
#endif
        LocalVisible=TryDock();
        if(LocalVisible)ReadSeconds+=Time.unscaledDeltaTime;
        if(Input.GetKeyDown(KeyCode.Tab))Advance();
    }
    static string Kind(Component m)
    {
        if(!m)return null;
        return m is MassGate?"gate":m is KineticCapacitor?"capacitor":m is MassEcho?"echo":m is RailShuttle?"shuttle":
            m is MassPrism?"prism":m is BodyRelay?"relay":m is MassLockStation?"masslock":m is VectorAirwell w?(w.tiered?"airwell":"freeflight"):
            m is ButtonController?"pressure":m is PlatformAndRockController?"counterweight":m is ElevatorController?"elevator":
            m is SpringController?"spring":m is BalanceBridge?"bridge":m is CheckpointBeacon?"checkpoint":m is SignalBarrier?"linkeddoor":m is LevelGoal?"exit":null;
    }
    public static Rect Project(Bounds b)
    {
        var camera=Camera.main;if(!camera)return default;
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        var a=camera.WorldToScreenPoint(b.min);var z=camera.WorldToScreenPoint(b.max);
        var margin=new Vector2((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f);
        return new Rect((a.x-margin.x)/scale,(Screen.height-z.y-margin.y)/scale,(z.x-a.x)/scale,(z.y-a.y)/scale);
    }
    bool Clear(Rect candidate)
    {
        var body=Project(player.GetComponent<Collider2D>().bounds);body.xMin-=18;body.xMax+=18;body.yMin-=18;body.yMax+=18;
        if(candidate.Overlaps(body))return false;
        foreach(var r in art)if(r && r.enabled && r.gameObject.activeInHierarchy && candidate.Overlaps(Project(r.bounds)))return false;
        var old=EnvironmentalGuidance.Instance;
        if(old && old.View && old.View.Visible)
        {
            var r=old.Placement;
            if(candidate.Overlaps(Project(new Bounds(r.center,new Vector3(r.width,r.height,1)))))return false;
        }
        return true;
    }
    bool TryDock()
    {
        if(art==null)art=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(r=>r.sortingOrder>=4 && !r.GetComponentInParent<PlayerController>()).ToArray();
        if(Panel.width>0 && Clear(Panel))return true;
#if UNITY_EDITOR
        if(!CompactLayoutAudit)
#endif
        foreach(float y in new[]{150f,190f,220f,250f,280f,310f,330f})
            foreach(float x in new[]{1030f,40f})
            {var rect=new Rect(x,y,530,430);if(Clear(rect)){Panel=rect;return true;}}
        // Tight rooms use one large illustration per page, rather than shrinking
        // the full card's art, text or keycaps to unreadable sizes.
        foreach(float y in new[]{150f,190f,230f,270f,310f,350f,380f})
            foreach(float x in new[]{40f,1200f,230f,1010f,290f,320f,380f,860f})
#if UNITY_EDITOR
            if(!CompactLayoutAudit || !WideCompactAudit)
#endif
            {var rect=new Rect(x,y,360,380);if(Clear(rect)){Panel=rect;return true;}}
        foreach(float y in new[]{150f,190f,230f,270f,310f,350f,410f})
            foreach(float x in new[]{40f,1080f,230f,890f})
            {var rect=new Rect(x,y,480,350);if(Clear(rect)){Panel=rect;return true;}}
        return false;
    }
#if UNITY_EDITOR
    public bool CompactLayoutAudit;
    public bool WideCompactAudit;
    public bool AuditDock(){Panel=default;return TryDock();}
#endif
}
