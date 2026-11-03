using System.Collections.Generic;
using UnityEngine;

/// <summary>Discovered field notes are opt-in; automatic lessons are world-space and never pause play.</summary>
public static class TutorialDirector
{
    public class Page
    {
        public string id, title, body, hint;
        public Page(string id, string title, string body, string hint)
        {
            this.id = id;
            this.title = title;
            this.body = body;
            this.hint = hint;
        }
    }
    public static Page Current
    {
        get; private set;
    }
#if UNITY_EDITOR
    public static bool TestBypass;
#endif
    static readonly Queue<Page> pages = new Queue<Page>();
    static FlowScreen returnScreen;
    public static int Remaining => pages.Count;
    public static bool Replaying { get; private set; }
    public static int GuideCount => AvailableIds().Count;
    static readonly Dictionary<string, Page> guides = new Dictionary<string, Page>
    {
        { "jump", new Page("jump","Mind the gap.","SPACE  ·  JUMP|HOLD  ·  HIGHER|RELEASE  ·  SHORTER","Try a jump where you have room to land.") },
        { "heat", new Page("heat","Heat changes the rhythm.","HEAT  ·  FOUNDRY|REST  ·  GAIN FASTER|MOVE  ·  LOSE SLOWER","Heat makes resting add weight faster and movement shed weight more slowly.") },
        { "cold", new Page("cold","Cold changes the rhythm.","COLD  ·  OBSERVATORY|MOVE  ·  LOSE FASTER|REST  ·  GAIN SLOWER","Cold makes movement shed weight faster and resting add weight more slowly.") },
        { "masslock", new Page("masslock","Hold your shape. Plan your next one.","E  ·  HOLD YOUR SHAPE|MOVE / EAT  ·  STORE|Q  ·  APPLY + RELEASE","Keep a needed shape through a crossing. Movement and food store your next change. Q applies it when there is room.") },
        { "movement", new Page("movement","Find your feet.","A  ·  LEFT|D  ·  RIGHT|ARROWS WORK TOO","Try moving. The markings ahead introduce the next action when you need it.") },
        { "food", new Page("food","Collect first. Eat when needed.","F  ·  COLLECT|1  ·  GROW|2  ·  SHRINK","The number beside each food is your supply. Collecting does not eat it.") },
        { "mass", new Page("mass","Three shapes. Three uses.","S  ·  QUICK + SMALL|M  ·  HOLD + BALANCE|L  ·  PRESS + LOWER","Your size changes what you can reach and what you can move.") },
        { "metabolism", new Page("metabolism","Moving loses weight. Resting adds it.","MOVE  ·  GET LIGHTER|REST  ·  GET HEAVIER|TRY THE RHYTHM","Keep moving to get lighter. Rest to get heavier. Discover the rhythm by trying it.") },
        { "score", new Page("score","A quicker escape earns more.","TIME  ·  BONUS|EXPLORE  ·  LEARN|ESCAPE  ·  EARN","A quicker escape earns a larger time bonus. Pauses stop the clock.") },
        { "duct", new Page("duct","Get small before the low passage.","2  ·  BECOME LIGHT|S  ·  FIT THROUGH|GROW AFTER EXIT","You cannot grow into a low ceiling. Save energy food for the open space ahead.") },
        { "pressure", new Page("pressure","A heavy body calls the lift.","L  ·  PRESS THE PLATE|WAIT FOR THE LIFT|BOARD  ·  RIDE UP","Jump onto the plate. Once called, the lift waits below until you board.") },
        { "counterweight", new Page("counterweight","Open it. Step off. Race through.","L  ·  RAISE THE CRATE|STEP OFF  ·  IT CLOSES|2  ·  SHRINK + RUN","M holds still while aboard. Missed the gap? Return left and press the plate again.") },
        { "elevator", new Page("elevator","Choose your height, then hold it.","S  ·  RISE|M  ·  HOLD STILL|L  ·  DESCEND","Eat on the platform to change direction. Keep watching your weight.") },
        { "spring", new Page("spring","Lighter bodies bounce higher.","S  ·  HIGH BOUNCE|M  ·  MEDIUM BOUNCE|L  ·  LOW BOUNCE","Land on the spring to bounce. Use 2 before a high jump.") },
        { "bridge", new Page("bridge","Balance opposite the crate.","S  ·  RISE|M  ·  LEVEL|L  ·  SINK","Stand opposite the crate. Match its mass, then jump across.") },
        { "checkpoint", new Page("checkpoint","The blue beacon saves your place.","TOUCH THE BEACON|KEEP FOOD + MASS|R  ·  RETRY","Press ENTER on the retry screen. Checkpoints last for this chapter run.") },
        { "capacitor", new Page("capacitor","Store impact. Spend it on a bridge.","L  ·  JUMP + LAND|IMPACT  ·  CHARGE|2  ·  LIGHT CROSSING","Standing still will not charge it. Heavy bodies drain the bridge faster. Jump again to recharge.") },
        { "airwell", new Page("airwell","One wind shaft. Three landing heights.","S  ·  HIGH ROUTE|M  ·  MIDDLE ROUTE|L  ·  LOW ROUTE","Use 1 / 2 in flight. The wind moves you to your body’s tier. A / D exits sideways.") },
        { "freeflight", new Page("freeflight","Free flight. Stop at any height.","S  ·  RISE|M  ·  HOLD HEIGHT|L  ·  DESCEND","This arrow-marked wind shaft has no fixed tiers. Use 1 / 2 to brake in midair, then A / D to exit.") },
        { "echo", new Page("echo","Leave your weight behind.","L + E  ·  RECORD|2  ·  SHRINK + LEAVE|CROSS IN TIME","The echo powers the gate with the matching E code. Record, shrink, and cross before it fades.") },
        { "shuttle", new Page("shuttle","Your body steers the carriage.","S  ·  TRAVEL RIGHT|M  ·  BRAKE + DOCK|L  ·  TRAVEL LEFT","Follow the line's lit route markers. Brake with medium at the cyan dock.") },
        { "prism", new Page("prism","Change mass. Route the light.","S  ·  UPPER RECEIVER|M  ·  MIDDLE RECEIVER|L  ·  LOWER RECEIVER","All three receivers power locks with the matching P code and count toward the exit. Finished receivers stay lit.") },
        { "relay", new Page("relay","Link every route to the exit.","READ THE BODY BADGE|HOLD THE SHAPE|LINK THE ROUTES","Each lit R node adds an exit link. Match the R label named in the HUD. The exit needs every required link.") },
        { "chapter_circuit", new Page("chapter_circuit","Restore the exit circuit.","MATCH R BADGES|FILL EXIT LINKS|UNLOCK THE EXIT","The HUD counter names the next missing exit requirement. R numbers match the nodes on the route.") },
        { "optics_circuit", new Page("optics_circuit","Three beams. One power circuit.","ROUTE THREE BEAMS|LIGHT ALL RECEIVERS|POWER MATCHING LOCKS","Prism receivers use S, M and L. Matching P codes identify the locks powered by their circuit.") },
        { "linkeddoor", new Page("linkeddoor","Find what powers this gate.","READ THE LINK CODE|FIND ITS SOURCE|RESTORE POWER","E codes link echoes to gates; P codes link optical circuits. An EXIT circuit needs all its named route links.") },
        { "gate", new Page("gate","Match. Stand still. Scan.","READ THE LIT BODY|HOLD STILL  ·  SCAN|GREEN  ·  PROCEED","Wrong size? Follow the food icon. Moving or changing size restarts the scan.") }
    };
    public static string SeenKey(string id) => "Fit.Guide.v4." + id;
    public static void Discover(string id) { PlayerPrefs.SetInt("Fit.Discovered.v4." + id, 1); }
    public static bool Discovered(string id) => PlayerPrefs.GetInt("Fit.Discovered.v4." + id, 0) != 0;
    public static void Learned(string id) { Discover(id); PlayerPrefs.SetInt(SeenKey(id), 1); PlayerPrefs.Save(); }
    static List<string> AvailableIds()
    {
        var result = new List<string>();
        foreach (string id in new[] { "movement", "jump", "food", "mass", "metabolism", "heat", "cold", "score", "duct", "chapter_circuit", "optics_circuit", "linkeddoor", "pressure", "counterweight", "elevator", "spring", "bridge", "checkpoint", "gate", "capacitor", "airwell", "freeflight", "relay", "echo", "shuttle", "prism", "masslock" })
            if (id == "movement" || Discovered(id) || Seen(id)) result.Add(id);
        return result;
    }
    public static bool Seen(string id) => PlayerPrefs.GetInt(SeenKey(id), 0) != 0;
    public static void ClearProgress()
    {
        FirstEncounterGuide.ClearProgress();
        foreach (string id in guides.Keys)
            for (int version = 1; version <= 4; version++)
                PlayerPrefs.DeleteKey("Fit.Guide.v" + version + "." + id);
        foreach (string id in guides.Keys) PlayerPrefs.DeleteKey("Fit.Discovered.v4." + id);
        for (int stage = 1; stage <= GameFlow.ChapterCount; stage++)
        {
            PlayerPrefs.DeleteKey(SeenKey("annex-" + stage));
            for (int index = 0; index < 2; index++)
                PlayerPrefs.DeleteKey(SeenKey("record-" + stage + "-" + index));
        }
        PlayerPrefs.DeleteKey("Fit.Guide.v4.ambient");
        PlayerPrefs.DeleteKey("Fit.Discovered.v4.ambient");
        Reset();
    }
    public static void Reset()
    {
        pages.Clear();
        Current = null;
        Replaying = false;
    }
    public static void Basics(bool replay = false)
    {
        if (!replay) { GameFlow.Instance.Resume(); return; }
        Reset(); Replaying = true; returnScreen = GameFlow.Instance.Screen;
        foreach (string id in AvailableIds()) pages.Enqueue(guides[id]);
        Advance();
    }
    public static void Show(string id, string title, string body, string hint, bool force = false)
    {
        if(FirstEncounterGuide.Instance && FirstEncounterGuide.Manages(id)){FirstEncounterGuide.Instance.Request(id);return;}
        // Research marks also stay in the world. Modal reading is only a deliberate menu action.
        if (EnvironmentalGuidance.Instance) EnvironmentalGuidance.Instance.Request(id);
    }
    public static void Trigger(string id)
    {
        if(FirstEncounterGuide.Instance && FirstEncounterGuide.Manages(id)){FirstEncounterGuide.Instance.Request(id);return;}
        if (EnvironmentalGuidance.Instance) EnvironmentalGuidance.Instance.Request(id);
    }
    public static void Advance()
    {
        GameAudio.Play("guide_page", .35f);
        if (Current != null)
            PlayerPrefs.SetInt(SeenKey(Current.id), 1);
        Current = pages.Count > 0 ? pages.Dequeue() : null;
        if (Current == null) Replaying = false;
        GameFlow.Instance.SetScreen(Current != null ? FlowScreen.Tutorial : returnScreen);
        PlayerPrefs.Save();
    }
    public static void CloseReplay()
    {
        if (!Replaying) return;
        Reset();
        GameFlow.Instance.SetScreen(returnScreen);
    }
    public static void Point(string id, Vector3 position, float radius = 4, Component owner = null, int phase = 0)
    {
        foreach (var existing in Object.FindObjectsByType<TutorialPoint>())
            if (existing.id == id && existing.phase == phase && (!existing.Owner || existing.Owner == owner) && (existing.transform.position-position).sqrMagnitude < 16)
            { if (owner) existing.Owner = owner; existing.radius = Mathf.Max(existing.radius,radius); return; }
        var go = new GameObject("Guide " + id); go.transform.position = position;
        var guide = go.AddComponent<TutorialPoint>(); guide.id = id; guide.radius = radius; guide.Owner = owner; guide.phase = phase;
    }
    public static void Install(int stage)
    {
        if (stage == 1)
        {
            Point("jump", new Vector3(17, 1), 5);
            Point("metabolism", new Vector3(60, 1), 6);
            Point("duct", new Vector3(34, 1), 5);
        }
        foreach (var m in Object.FindObjectsByType<ButtonController>()) Point("pressure", m.transform.position, 6, m);
        foreach (var m in Object.FindObjectsByType<PlatformAndRockController>()) Point("counterweight", m.platformUpPosition, 7, m);
        foreach (var m in Object.FindObjectsByType<ElevatorController>()) Point("elevator", m.transform.position, 6, m);
        foreach (var m in Object.FindObjectsByType<SpringController>()) Point("spring", m.transform.position, 6, m);
        foreach (var m in Object.FindObjectsByType<BalanceBridge>()) Point("bridge", m.transform.position+Vector3.left*4, 6, m);
        foreach (var m in Object.FindObjectsByType<CheckpointBeacon>()) Point("checkpoint", m.transform.position, 4, m);
        foreach (var m in Object.FindObjectsByType<LabDevice>())
        {
            string id = m is MassGate ? "gate" : m is KineticCapacitor ? "capacitor" : m is MassEcho ? "echo" : m is RailShuttle ? "shuttle" : m is MassPrism ? "prism" : m is MassLockStation ? "masslock" : m is BodyRelay ? "relay" : m is VectorAirwell ? (((VectorAirwell)m).tiered ? "airwell" : "freeflight") : null;
            if (id != null) Point(id, m.transform.position, 6, m);
            if (m is RailShuttle rail)
            {
                Point("shuttle", new Vector3(rail.dock, rail.transform.position.y+1), 7, rail, 1);
                if (rail.requireReturn) Point("shuttle", new Vector3(rail.right, rail.transform.position.y+1), 5, rail, 2);
            }
            if(m is VectorAirwell wind && !wind.tiered)
                Point("freeflight",wind.transform.position+Vector3.up*(wind.height-3),7,wind,1);
        }
        foreach (var food in Object.FindObjectsByType<FoodVisual>()) Point("food", food.transform.position, 4, food);
        foreach (var door in Object.FindObjectsByType<SignalBarrier>()) Point("linkeddoor",door.transform.position,6,door);
        foreach (var goal in Object.FindObjectsByType<LevelGoal>()) { Point("score", goal.transform.position, 4, goal);Point("exit",goal.transform.position,5,goal); }
    }
}
public class TutorialPoint : MonoBehaviour
{
    public string id;
    public float radius = 4;
    public int phase;
    public Component Owner;
    public bool UsedThisVisit;
}
