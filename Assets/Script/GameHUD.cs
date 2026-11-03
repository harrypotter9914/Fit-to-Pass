using UnityEngine;
using System.Collections.Generic;

/// <summary>Illustrated, centered laboratory UI. World controls remain in GameFlow.</summary>
public partial class GameHUD : MonoBehaviour
{
    readonly Color ink = new Color(.08f, .19f, .22f), muted = new Color(.32f, .40f, .40f), paper = new Color(.98f, .95f, .86f), teal = new Color(.12f, .40f, .38f), copper = new Color(.74f, .43f, .21f);
    readonly Dictionary<(int, Color, TextAnchor), GUIStyle> styles = new Dictionary<(int, Color, TextAnchor), GUIStyle>();
    Font font;
    GameFlow flow;
    float volume;
    Rect canvas = new Rect(0, 0, 1600, 900);
    void Start()
    {
        flow = GetComponent<GameFlow>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 24);
        volume = AudioListener.volume;
    }
    GUIStyle Style(int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var key = (size, color, align);
        if (!styles.TryGetValue(key, out var s))
        {
            s = new GUIStyle(GUI.skin.label) { font = font, fontSize = size, alignment = align, wordWrap = true, richText = false, padding = new RectOffset(), margin = new RectOffset() };
            s.normal.textColor = color;
            s.hover.textColor = s.active.textColor = s.focused.textColor = color;
            s.onNormal.textColor = s.onHover.textColor = s.onActive.textColor = s.onFocused.textColor = color;
            styles[key] = s;
        }
        return s;
    }
#if UNITY_EDITOR
    public static readonly HashSet<string> LayoutWarnings = new HashSet<string>();
#endif
    void Label(Rect r, string value, int size = 22, TextAnchor align = TextAnchor.MiddleLeft, Color? color = null)
    {
        var style = Style(size, color ?? ink, align);
#if UNITY_EDITOR
        if (style.CalcHeight(new GUIContent(value), r.width) > r.height + 2)
            LayoutWarnings.Add(flow.Screen + " | " + value + " | " + r + " needs " + style.CalcHeight(new GUIContent(value), r.width));
#endif
        GUI.Label(r, value, style);
    }
    void Fill(Rect r, Color color)
    {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
    void Sprite(Rect r, Sprite s)
    {
        IllustratedUI.Image(r, s);
    }
    void Card(Rect r, int piece = 0)
    {
        if (piece == 3)
            Sprite(r, Art.Cell("InterfaceKit", 2, 2, 3));
        else
            IllustratedUI.Frame(r, Art.Cell(piece == 0 ? "ModalFrame" : "ControlFrame", 1, 1, 0), piece == 0);
    }
    bool Button(Rect r, string title, bool primary = false, bool enabled = true)
    {
        bool hover = r.Contains(Event.current.mousePosition) && enabled;
        var old = GUI.color;
        GUI.color = !enabled ? new Color(.65f, .65f, .65f, .85f) : hover ? Color.white : primary ? new Color(.78f, .96f, .9f) : new Color(.94f, .96f, .96f);
        Card(r, 1);
        GUI.color = old;
        Rect content = new Rect(r.x + 34, r.y + 2, r.width - 68, r.height - 4);
        int size = 22;
        while (size > 14 && Style(size, ink).CalcSize(new GUIContent(title)).x > content.width)
            size--;
        Label(content, title, size, TextAnchor.MiddleCenter, enabled ? ink : muted);
        GUI.enabled = enabled && !flow.Loading;
        bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none);
        GUI.enabled = true;
        if (hit)
            GameAudio.Play("ui_click", .35f);
        return hit;
    }
    void Background(string name = "MenuV2")
    {
        var t = Art.Texture(name);
        if (t)
            GUI.DrawTexture(canvas, t, ScaleMode.ScaleAndCrop);
        else
            Fill(canvas, paper);
    }
    void Veil(float alpha = .6f) => Fill(canvas, new Color(.04f, .10f, .12f, alpha));
    void Heading(string eyebrow, string title, Rect area)
    {
        Label(new Rect(area.x, area.y, area.width, 32), eyebrow, 16, TextAnchor.MiddleCenter, copper);
        Label(new Rect(area.x, area.y + 37, area.width, 70), title, 40, TextAnchor.MiddleCenter);
    }
    void Key(Rect r, string key)
    {
        Card(r, 3);
        Label(r, key, Mathf.RoundToInt(r.height * .42f), TextAnchor.MiddleCenter);
    }
    void OnGUI()
    {
        if (!flow)
            return;
        float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
        Vector2 margin = new Vector2((Screen.width - 1600 * scale) * .5f, (Screen.height - 900 * scale) * .5f);
        canvas = new Rect(-margin.x / scale, -margin.y / scale, Screen.width / scale, Screen.height / scale);
        GUI.matrix = Matrix4x4.TRS(margin, Quaternion.identity, new Vector3(scale, scale, 1));
        switch (flow.Screen)
        {
            case FlowScreen.Menu:
                Menu();
                break;
            case FlowScreen.NewGameConfirm:
                NewGameConfirmation();
                break;
            case FlowScreen.Stages:
                Stages();
                break;
            case FlowScreen.Intro:
                Intro();
                break;
            case FlowScreen.Playing:
                HUD();
                DeviceOverlay();
                FirstEncounterOverlay();
                break;
            case FlowScreen.Paused:
                Pause();
                break;
            case FlowScreen.Settings:
                Settings();
                break;
            case FlowScreen.Dead:
                Result(false);
                break;
            case FlowScreen.Complete:
                Result(true);
                break;
            case FlowScreen.Ending:
                Ending();
                break;
            case FlowScreen.Credits:
                Credits();
                break;
            case FlowScreen.Tutorial:
                Tutorial();
                break;
            case FlowScreen.Discovery:
                Discovery();
                break;
            case FlowScreen.Opening:
                Opening();
                break;
        }
        if (flow.Screen == FlowScreen.Playing && !LockedRouteFeedback() && flow.NoticeUntil > Time.unscaledTime)
        {
            bool teaching=FirstEncounterGuide.Instance && FirstEncounterGuide.Instance.LocalVisible;
            var notice=teaching?BesideEncounter(new Rect(600,137,400,72)):new Rect(490,130,620,60);
            Card(notice,1);
            EncounterLabel(new Rect(notice.x+20,notice.y+7,notice.width-40,notice.height-14),flow.Notice,20,TextAnchor.MiddleCenter);
        }
        if (flow.Loading)
        {
            Veil(.97f);
            Label(new Rect(350, 380, 900, 120), "ENTERING THE FACILITY...", 30, TextAnchor.MiddleCenter, paper);
        }
        GUI.matrix = Matrix4x4.identity;
    }
    void Menu()
    {
        Background();
        var logo = Art.Texture("TitleLogo");
        if (logo) GUI.DrawTexture(new Rect(480, 63, 640, 300), logo, ScaleMode.ScaleToFit);
        Label(new Rect(450, 369, 700, 38), "Change your shape. Find your way out.", 24, TextAnchor.MiddleCenter);
        if (Button(new Rect(550, 430, 500, 63), "NEW GAME", true)) flow.RequestNewGame();
        if (Button(new Rect(550, 504, 500, 63), flow.HasProgress ? "CONTINUE  /  CHAPTER " + flow.ContinueStage : "CONTINUE  /  NO SAVE", false, flow.HasProgress))
            flow.BeginCampaign(flow.ContinueStage);
        if (Button(new Rect(550, 578, 500, 60), "CHOOSE A CHAPTER")) flow.SetScreen(FlowScreen.Stages);
        if (Button(new Rect(550, 651, 242, 56), "SETTINGS")) flow.OpenSettings();
        if (Button(new Rect(808, 651, 242, 56), "FIELD GUIDE")) TutorialDirector.Basics(true);
        if (Button(new Rect(550, 720, 242, 56), "REPLAY PROLOGUE")) flow.GetComponent<OpeningSequence>().Begin(true);
        if (Button(new Rect(808, 720, 242, 56), "CREDITS")) flow.SetScreen(FlowScreen.Credits);
        Label(new Rect(480, 838, 640, 32), "FIT TO PASS 2.4  ·  15 CHAPTERS  ·  YIBO WANG", 16, TextAnchor.MiddleCenter, paper);
        if (Button(new Rect(1390, 817, 160, 51), "QUIT")) Application.Quit();
    }
    void NewGameConfirmation()
    {
        Background(); Veil(.55f);
        Card(new Rect(320, 165, 960, 560));
        Heading("A FRESH EXPERIMENT", "Start a new game?", new Rect(400, 205, 800, 110));
        Sprite(new Rect(445, 355, 110, 140), Art.Cell("Props", 3, 2, 5));
        Label(new Rect(605, 333, 540, 57), "Your current campaign will be replaced.", 27);
        Label(new Rect(605, 405, 540, 108), @"Chapter progress, scores and guide history reset.
The opening story and chapter one begin again.
Your sound and display settings stay as they are.", 21);
        Label(new Rect(400, 534, 800, 36), "Cancel keeps your current game. ESC or ENTER also cancels.", 18, TextAnchor.MiddleCenter, muted);
        if (Button(new Rect(410, 603, 350, 63), "CANCEL", true)) flow.CancelNewGame();
        if (Button(new Rect(805, 603, 385, 63), "START NEW GAME")) flow.ConfirmNewGame();
    }
    int chapterPage;
    void Stages()
    {
        Background();
        Card(new Rect(90, 55, 1420, 790));
        Heading("EXPERIMENT ARCHIVE", CampaignMap.Regions[chapterPage] + " / CHOOSE A CHAPTER", new Rect(300, 77, 1000, 100));
        for (int i = chapterPage * 5; i < chapterPage * 5 + 5; i++)
        {
            float x = 170 + ((i - chapterPage * 5) % 3) * 430, y = 209 + ((i - chapterPage * 5) / 3) * 280;
            Card(new Rect(x, y, 400, 255));
            var bg = Resources.Load<Texture2D>("ChapterPreviews/Chapter" + (i + 1).ToString("00")) ?? Art.Texture(new[] { "Lab", "Heat", "Cold" }[CampaignMap.Region(i + 1)]);
            if (bg)
                GUI.DrawTexture(new Rect(x + 20, y + 19, 360, 109), bg, ScaleMode.ScaleAndCrop);
            Label(new Rect(x + 28, y + 137, 345, 32), (i + 1).ToString("00") + "  " + GameFlow.StageNames[i], 23, TextAnchor.MiddleCenter);
            string subtitle = CampaignMap.Summaries[i];
            Label(new Rect(x + 25, y + 170, 350, 28), subtitle, 16, TextAnchor.MiddleCenter, muted);
            bool unlocked = i < flow.Unlocked;
            if (Button(new Rect(x + 50, y + 203, 300, 42), unlocked ? "ENTER" : "LOCKED", unlocked, unlocked))
                flow.BeginCampaign(i + 1);
        }
        if (Button(new Rect(170, 782, 360, 53), "GREENHOUSE / 1–5", chapterPage == 0)) chapterPage = 0;
        if (Button(new Rect(620, 782, 360, 53), "FOUNDRY / 6–10", chapterPage == 1)) chapterPage = 1;
        if (Button(new Rect(1070, 782, 360, 53), "OBSERVATORY / 11–15", chapterPage == 2)) chapterPage = 2;
        if (Button(new Rect(1150, 535, 300, 53), "BACK TO TITLE"))
            flow.SetScreen(FlowScreen.Menu);
    }
    void Intro()
    {
        // Compatibility screen for old tools; chapter entry itself goes straight to play.
        Background(new[] { "Lab", "Heat", "Cold" }[CampaignMap.Region(flow.Stage)]);
        Card(new Rect(350, 280, 900, 300));
        Heading(CampaignMap.Regions[CampaignMap.Region(flow.Stage)], GameFlow.StageNames[flow.Stage-1], new Rect(420,310,760,110));
        if (Button(new Rect(550,480,500,65), "STEP INSIDE", true)) flow.BeginGameplay();
    }
    void HUD(bool interactive = true)
    {
        var p = flow.Player;
        if (!p)
            return;
        Card(new Rect(32, 24, 304, 100), 2);
        Label(new Rect(64, 39, 240, 28), "TIME  " + Clock(flow.Elapsed), 20);
        Label(new Rect(64, 76, 240, 27), "TIME BONUS  " + flow.Score.ToString("N0"), 17, TextAnchor.MiddleLeft, flow.Score < 2000 ? copper : muted);
        Card(new Rect(566, 24, 468, 68), 1);
        Label(new Rect(600, 35, 400, 46), flow.Stage.ToString("00") + "  ·  " + GameFlow.StageNames[flow.Stage - 1] + "", 19, TextAnchor.MiddleCenter);
        var circuit = Object.FindAnyObjectByType<RouteCircuit>();
        if (circuit) Label(new Rect(420, 100, 760, 28), circuit.NextObjective, 17, TextAnchor.MiddleCenter);
        if (Button(new Rect(1400, 24, 168, 68), "PAUSE", false, interactive))
            flow.SetScreen(FlowScreen.Paused);
        FoodCard(new Rect(30, 778, 240, 100), true, p.JunkCount);
        FoodCard(new Rect(286, 778, 240, 100), false, p.HealthyCount);
        Card(new Rect(560, 778, 548, 102), 2);
        string[] names = { "LIGHT", "MEDIUM", "HEAVY" };
        Label(new Rect(592, 791, 200, 26), "MASS / " + names[p.Size], 18);
        string direction = Mathf.Abs(p.HorizontalInput) > .1f ? "MOVING: LOSING MASS" : "RESTING: GAINING MASS";
        var hold = p.GetComponent<MassLock>();
        if (hold && hold.Locked)
            direction = hold.WaitingForSpace ? "LOCK HELD: NEED OPEN SPACE" : "LOCKED " + hold.Remaining.ToString("0.0") + "s  /  Q RELEASE";
        Label(new Rect(812, 791, 264, 26), direction, 15, TextAnchor.MiddleRight, muted);
        Color[] colors = { teal, new Color(.65f, .68f, .48f), copper };
        for (int i = 0; i < 3; i++)
            Fill(new Rect(592 + i * 160, 825, 156, 14), colors[i]);
        float needle = 592 + (p.Weight / 17f) * 476;
        Fill(new Rect(needle - 2, 821, 4, 22), ink);
        if (hold && hold.Locked)
        {
            float pending = 592 + hold.PendingWeight / 17f * 476;
            Fill(new Rect(pending - 3, 817, 6, 6), copper);
            Card(new Rect(560, 716, 548, 54), 1);
            Label(new Rect(589, 724, 490, 36), "ON RELEASE: " + hold.PendingWeight + " / 17  ·  " + names[hold.PendingWeight < 6 ? 0 : hold.PendingWeight < 12 ? 1 : 2], 17, TextAnchor.MiddleCenter, copper);
        }
        for (int i = 0; i < 3; i++)
            Label(new Rect(592 + i * 160, 845, 156, 23), new[] { "S  LIGHT", "M  MEDIUM", "L  HEAVY" }[i], 15, TextAnchor.MiddleCenter, muted);
        if (p.HasNearbyFood)
        {
            Card(new Rect(1160, 792, 288, 66), 1);
            Label(new Rect(1192, 804, 224, 42), "F  COLLECT FOOD", 20, TextAnchor.MiddleCenter);
        }
        if (Button(new Rect(1480, 792, 80, 66), "?", false, interactive))
            TutorialDirector.Basics(true);
        if (p.GrowthBlocked && !(hold && hold.Locked))
        {
            Card(new Rect(500, 684, 600, 72), 1);
            Label(new Rect(534, 696, 532, 46), "Not enough headroom. Leave the duct before growing.", 18, TextAnchor.MiddleCenter);
        }
    }
    void FoodCard(Rect r, bool gain, int count)
    {
        Card(r, 2);
        Sprite(new Rect(r.x + 27, r.y + 25, 49, 49), Art.Cell("Icons", 2, 2, gain ? 0 : 1));
        Key(new Rect(r.x + 91, r.y + 15, 38, 38), gain ? "1" : "2");
        Label(new Rect(r.x + 144, r.y + 21, 68, 28), count + " left", 20);
        Label(new Rect(r.x + 90, r.y + 59, 119, 26), gain ? "GROW" : "SHRINK", 18, TextAnchor.MiddleLeft, gain ? copper : teal);
    }
    void Pause()
    {
        HUD(false);
        Veil(.55f);
        Card(new Rect(385, 110, 830, 682));
        Heading("EXPERIMENT PAUSED", "Take a breath.", new Rect(500, 149, 600, 114));
        Label(new Rect(500, 281, 600, 54), "Your weight and timer are paused.", 22, TextAnchor.MiddleCenter, muted);
        if (Button(new Rect(570, 369, 460, 67), "RESUME", true))
            flow.Resume();
        if (Button(new Rect(461, 461, 326, 64), "FIELD GUIDE"))
            TutorialDirector.Basics(true);
        if (Button(new Rect(813, 461, 326, 64), "SETTINGS"))
            flow.OpenSettings();
        if (Button(new Rect(461, 552, 326, 64), "RESTART CHAPTER"))
            flow.StartStage(flow.Stage);
        if (Button(new Rect(813, 552, 326, 64), "BACK TO TITLE"))
            flow.MainMenu();
        Label(new Rect(510, 669, 580, 45), "S RISES  /  M HOLDS  /  L DESCENDS", 23, TextAnchor.MiddleCenter, muted);
    }
    void Settings()
    {
        Background();
        Veil(.25f);
        Card(new Rect(215, 92, 1170, 722));
        Heading("PREFERENCES", "Settings & controls", new Rect(400, 129, 800, 115));
        volume = AudioSlider("MASTER VOLUME", 274, volume);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Fit.Volume", volume);
        GameAudio.MusicVolume = AudioSlider("MUSIC", 359, GameAudio.MusicVolume);
        GameAudio.EffectsVolume = AudioSlider("SOUND EFFECTS", 444, GameAudio.EffectsVolume);
        if (Button(new Rect(305, 549, 423, 61), Screen.fullScreen ? "DISPLAY / FULLSCREEN" : "DISPLAY / WINDOWED"))
            Screen.fullScreen = !Screen.fullScreen;
        bool reduced = PlayerPrefs.GetInt("Fit.ReducedMotion", 0) == 1;
        if (Button(new Rect(305, 629, 423, 61), reduced ? "REDUCED MOTION / ON" : "REDUCED MOTION / OFF"))
            PlayerPrefs.SetInt("Fit.ReducedMotion", reduced ? 0 : 1);
        ControlReference();
        if (Button(new Rect(809, 613, 435, 58), "REPLAY THE FIELD GUIDE"))
            TutorialDirector.Basics(true);
        if (Button(new Rect(610, 713, 380, 61), "SAVE & BACK", true))
        {
            PlayerPrefs.Save();
            flow.CloseSettings();
        }
    }
    float AudioSlider(string title, float y, float value)
    {
        Label(new Rect(311, y, 310, 35), title, 22);
        Label(new Rect(626, y, 90, 35), Mathf.RoundToInt(value * 100) + "%", 22, TextAnchor.MiddleRight, teal);
        return GUI.HorizontalSlider(new Rect(315, y + 46, 400, 24), value, 0, 1);
    }
    void Tutorial()
    {
        var page = TutorialDirector.Current;
        if (page == null)
            return;
        if (flow.Player)
            HUD(false);
        else
            Background();
        Veil(.48f);
        Card(new Rect(224, 100, 1152, 702));
        Heading(TutorialDirector.Replaying ? "FIELD GUIDE / " + (TutorialDirector.GuideCount - TutorialDirector.Remaining) + " OF " + TutorialDirector.GuideCount : "FIELD GUIDE", page.title, new Rect(370, 129, 880, 112));
        if (DrawGuidePanels(page))
        {
            Label(new Rect(315, 622, 970, 65), page.hint, 22, TextAnchor.MiddleCenter, teal);
            if (TutorialDirector.Replaying && Button(new Rect(270, 708, 250, 61), "CLOSE GUIDE"))
                TutorialDirector.CloseReplay();
            if (Button(new Rect(575, 708, 450, 61), TutorialDirector.Remaining > 0 ? "NEXT  /  ENTER" : "GOT IT  /  ENTER", true))
                TutorialDirector.Advance();
            return;
        }
        string id = page.id;
        if (id == "food")
        {
            Sprite(new Rect(313, 305, 102, 102), Art.Cell("Icons", 2, 2, 0));
            Key(new Rect(425, 325, 65, 65), "1");
            Sprite(new Rect(313, 457, 102, 102), Art.Cell("Icons", 2, 2, 1));
            Key(new Rect(425, 477, 65, 65), "2");
        }
        else if (id == "pressure" || id == "spring" || id == "bridge" || id == "counterweight" || id == "elevator")
        {
            int index = id == "spring" ? 2 : id == "pressure" ? 1 : id == "bridge" || id == "elevator" ? 0 : 3;
            Sprite(new Rect(303, 337, 223, 210), Art.Cell("Props", 3, 2, index));
        }
        else
            Sprite(new Rect(318, 313, 181, 260), Art.Cell("Subject", 4, 2, 0, false, 300));
        Label(new Rect(587, 280, 683, 329), page.body, 23, TextAnchor.UpperLeft);
        Label(new Rect(355, 630, 890, 44), page.hint, 20, TextAnchor.MiddleCenter, teal);
        if (Button(new Rect(575, 708, 450, 61), TutorialDirector.Remaining > 0 ? "NEXT  /  ENTER" : "GOT IT  /  ENTER", true))
            TutorialDirector.Advance();
    }
    void Opening()
    {
        var opening = flow.GetComponent<OpeningSequence>();
        if (!opening.Running)
        {
            Background("Story0" + (opening.Page + 1));
            string[] titles = { "Another unfinished workday", "An experiment without an invitation", "You wake up different", "Change might be the way out" };
            string[] captions = { "Overworked and exhausted, you kept postponing rest until tomorrow.", "A mysterious organization brings you into its facility. An ordinary life is left on the other side of the door.", "You have become a prototype that can change its body. Size, weight and movement now respond to your choices.", "Press the switches. Slip through the gaps. Balance the machinery. Use their experiment to find your way out." };
            Card(new Rect(210, 661, 1180, 192));
            Label(new Rect(310, 693, 980, 43), titles[opening.Page], 29, TextAnchor.MiddleCenter);
            Label(new Rect(315, 750, 970, 64), captions[opening.Page], 23, TextAnchor.MiddleCenter);
            float a = Mathf.Clamp01(1 - (opening.Elapsed % 5) / .6f);
            Fill(canvas, new Color(0, 0, 0, a));
        }
        else
        {
            Background("Lab");
            for (int tile = 0; tile < 13; tile++)
                Sprite(new Rect(tile * 128, 700, 128, 128), Art.Cell("TerrainV2", 3, 2, 0, false));
            float t = opening.Elapsed - 20;
            float x = Mathf.Lerp(180, 1420, Mathf.Clamp01(t / 4));
            int frame = 1 + (int)(t * 10) % 3;
            Sprite(new Rect(x, 510, 147, 196), Art.Cell("Subject", 4, 2, frame, false, 300));
            Label(new Rect(350, 825, 900, 56), "From here, the way out is yours to find.", 26, TextAnchor.MiddleCenter, paper);
            Fill(canvas, new Color(0, 0, 0, opening.Fade));
        }
        if (opening.Fade < .5f && Button(new Rect(1360, 32, 200, 53), "SKIP  /  ESC"))
            opening.Finish();
    }
    void Result(bool success)
    {
        Veil(.6f);
        Card(new Rect(300, 110, 1000, 680));
        Heading(success ? "CHAPTER COMPLETE" : "TRY ANOTHER WAY", success ? "A little closer to freedom." : flow.ScoreFailed ? "The experiment window closed." : "Try a different shape.", new Rect(440, 141, 720, 115));
        Label(new Rect(470, 286, 660, 67), success ? GameFlow.StageNames[flow.Stage - 1] + " / COMPLETE" : flow.ScoreFailed ? "Reach the exit with at least 1,000 points.\nRestart the chapter to reset the clock." : "Restore your position, weight and food at the latest checkpoint.", 24, TextAnchor.MiddleCenter, muted);
        Label(new Rect(390, 412, 820, 65), "TIME " + Clock(flow.Elapsed) + "     TIME BONUS " + flow.Score.ToString("N0"), 34, TextAnchor.MiddleCenter);
        Label(new Rect(450, 488, 700, 40), "12 points per second. Pauses and guides stop the clock.", 19, TextAnchor.MiddleCenter, muted);
        if (Button(new Rect(550, 584, 500, 66), success ? (flow.Stage == GameFlow.ChapterCount ? "STEP OUTSIDE" : "NEXT CHAPTER") : (flow.ScoreFailed ? "RESTART CHAPTER" : "RETRY CHECKPOINT"), true))
        {
            if (success)
                flow.Next();
            else
                flow.Retry();
        }
        if (Button(new Rect(610, 679, 380, 58), "BACK TO TITLE"))
            flow.MainMenu();
    }
    void Ending()
    {
        Background();
        Card(new Rect(345, 195, 910, 524));
        Heading("RELEASE AUTHORIZED", "Your life is waiting.", new Rect(450, 227, 700, 112));
        Label(new Rect(453, 392, 694, 162), "Six experiment sectors behind you.\nMore than one shape. More than one way through.\n\nThe life beyond these doors is still yours.", 25, TextAnchor.MiddleCenter);
        if (Button(new Rect(565, 605, 470, 66), "BACK TO TITLE", true))
            flow.MainMenu();
    }
    void Credits()
    {
        Background();
        Card(new Rect(325, 120, 950, 655));
        Heading("FIT TO PASS", "CREDITS", new Rect(440, 151, 720, 115));
        Label(new Rect(459, 298, 682, 284), "ORIGINAL DESIGN, STORY & FIRST THREE LEVELS\nYibo Wang\n\nRESTORATION & EXPANSION\nUnity 6, generated illustrations and synthesized audio\n\nTHE CORE IDEA\nUnderstand change. Find a way through in a different shape.", 25, TextAnchor.MiddleCenter);
        if (Button(new Rect(585, 670, 430, 62), "BACK TO TITLE", true))
            flow.SetScreen(FlowScreen.Menu);
    }
    static string Clock(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
}
