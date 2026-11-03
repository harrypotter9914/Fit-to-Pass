using UnityEngine;

/// <summary>Short visual instructions composed from the actual generated game sprites.</summary>
public partial class GameHUD
{
    bool DrawGuidePanels(TutorialDirector.Page page)
    {
        var captions = page.body.Split('|');
        if (captions.Length != 3)
            return false;
        float phase = PlayerPrefs.GetInt("Fit.ReducedMotion", 0) == 1 ? .5f : Mathf.Repeat(Time.unscaledTime * .45f, 1);
        for (int i = 0; i < 3; i++)
        {
            float x = 300 + i * 340, center = x + 150;
            Card(new Rect(x, 260, 300, 349), 1);
            Label(new Rect(x + 28, 276, 244, 26), "0" + (i + 1), 16, TextAnchor.MiddleCenter, copper);
            if(page.id=="chapter_circuit" || page.id=="optics_circuit" || page.id=="linkeddoor")
                EncounterArt(new Rect(x,310,300,215),new FirstEncounterGuide.Entry{Lesson=FirstEncounterGuide.Lessons[page.id],Chapter=flow.Stage},i);
            else GuidePicture(page.id, i, center, phase);
            Label(new Rect(x + 25, 541, 250, 51), captions[i], 23, TextAnchor.MiddleCenter);
        }
        return true;
    }

    void GuideActor(float x, float feet, float height, int frame = 0)
        => Sprite(new Rect(x - height * .4f, feet - height, height * .8f, height), Art.Cell("Subject", 4, 2, frame, false, 300));
    void GuideProp(int index, float x, float y, float width, float height)
        => Sprite(new Rect(x - width * .5f, y, width, height), Art.Cell("Props", 3, 2, index));
    void GuideFloor(float x, float y = 500)
        => GuideProp(0, x, y, 236, 30);
    void GuideArrow(float x, float y, string arrow)
        => Label(new Rect(x - 24, y, 48, 58), arrow, 44, TextAnchor.MiddleCenter, teal);
    void GuideKey(float x, float y, string key, float width = 54)
    {
        Card(new Rect(x - width * .5f, y, width, 46), 1);
        Label(new Rect(x - width * .5f + 8, y + 4, width - 16, 38), key, 21, TextAnchor.MiddleCenter);
    }
    void GuidePicture(string id, int i, float x, float phase)
    {
        float bounce = Mathf.Sin(phase * Mathf.PI);
        int run = PlayerPrefs.GetInt("Fit.ReducedMotion", 0) == 1 ? 2 : 1 + (int)(Time.unscaledTime * 8) % 3;
        switch (id)
        {
            case "masslock":
                GuideFloor(x);
                if (i == 0)
                {
                    Sprite(new Rect(x - 110, 445, 220, 55), Art.Cell("MassLockStation", 1, 1, 0));
                    GuideActor(x, 457, 86);
                    GuideKey(x + 83, 330, "E");
                }
                else if (i == 1)
                {
                    GuideActor(x - 35, 500, 86, run);
                    Sprite(new Rect(x + 23, 372, 63, 63), Art.Cell("Icons", 2, 2, 0));
                    GuideKey(x + 55, 450, "1");
                    Label(new Rect(x - 110, 316, 220, 40), "NOW S  /  NEXT M", 20, TextAnchor.MiddleCenter, teal);
                }
                else
                {
                    GuideActor(x, 500, 86 + phase * 45);
                    GuideKey(x - 82, 330, "Q");
                    GuideArrow(x + 86, 405, "↑");
                }
                break;
            case "movement":
                GuideFloor(x);
                GuideActor(x - 65 + phase * 130,500,96,run);
                GuideKey(x,327,i==0?"A":i==1?"D":"ARROWS",i==2?106:54);
                GuideArrow(x + (i==0?-90:90),420,i==0?"←":"→");
                break;
            case "jump":
                GuideFloor(x);
                GuideActor(x+25,500-bounce*(i==0?65:i==1?100:43),88,5);
                GuideKey(x-65,330,"SPACE",94); GuideArrow(x+86,414,"↑");
                break;
            case "heat":
            case "cold":
                GuideFloor(x);
                if(i==0)
                {
                    GuideActor(x,500,90);
                    Label(new Rect(x-105,322,210,44),id=="heat"?"HEAT":"COLD",30,TextAnchor.MiddleCenter,teal);
                    for(int mark=0;mark<5;mark++)
                        GuideArrow(x-96+mark*48,370+Mathf.Repeat(phase+mark*.2f,1)*55,id=="heat"?"↑":"↓");
                }
                else
                {
                    bool fast=(id=="heat" && i==1)||(id=="cold" && i==2);
                    float progress=fast?Mathf.Repeat(phase*1.8f,1):phase*.45f;
                    GuideActor(x,500,i==1?66+progress*50:116-progress*50,i==2?run:0);
                    GuideArrow(x+88,410,i==1?"↑":"↓");
                    Label(new Rect(x-110,322,220,40),fast?"FASTER":"SLOWER",23,TextAnchor.MiddleCenter,teal);
                }
                break;
            case "food":
                if (i == 0)
                {
                    GuideActor(x - 52, 475, 116);
                    Sprite(new Rect(x + 20, 372, 78, 78), Art.Cell("Icons", 2, 2, 0));
                    GuideArrow(x, 353, "←");
                    GuideKey(x + 52, 467, "F");
                }
                else
                {
                    Sprite(new Rect(x - 62, 332, 124, 124), Art.Cell("Icons", 2, 2, i - 1));
                    GuideKey(x, 470, i == 1 ? "1" : "2");
                }
                break;
            case "mass":
                GuideFloor(x);
                GuideActor(x, 500, new[] { 65f, 104f, 145f }[i], i == 0 ? run : 0);
                Label(new Rect(x - 60, 317, 120, 45), new[] { "S", "M", "L" }[i], 32, TextAnchor.MiddleCenter, teal);
                break;
            case "metabolism":
                GuideFloor(x);
                if (i < 2)
                {
                    GuideActor(x, 500, i == 0 ? 112 - phase * 40 : 72 + phase * 40, i == 0 ? run : 0);
                    GuideArrow(x + 86, 389, i == 0 ? "↓" : "↑");
                }
                else
                {
                    GuideActor(x-65,500,72+phase*32);
                    GuideActor(x+65,500,104-phase*32,run);
                    GuideArrow(x-65,350,"↑"); GuideArrow(x+65,350,"↓");
                }
                break;
            case "score":
                GuideFloor(x);
                if (i == 2)
                    GuideProp(4, x + 45, 359, 70, 141);
                GuideActor(i == 2 ? x - 48 : x - 55 + phase * 110, 500, 100, run);
                Label(new Rect(x - 115, 321, 230, 50), i == 0 ? "TIME BONUS" : i == 1 ? "FIND YOUR WAY" : "EXIT", 33, TextAnchor.MiddleCenter, teal);
                break;
            case "duct":
                GuideFloor(x);
                GuideProp(0, x, 403, 138, 42);
                GuideActor(x + (i - 1) * 90, 500, i == 2 ? 110 : 56, run);
                if (i == 0)
                    GuideKey(x - 87, 328, "2");
                GuideArrow(x, 334, "→");
                break;
            case "pressure":
                GuideFloor(x);
                if (i == 0)
                {
                    GuideProp(1, x, 478, 92, 28);
                    GuideActor(x, 478, 136);
                    GuideArrow(x + 88, 395, "↓");
                }
                else
                {
                    float y = i == 1 ? 370 + phase * 110 : 480 - phase * 110;
                    GuideProp(0, x, y, 145, 30);
                    if (i == 2)
                        GuideActor(x, y, 68);
                    GuideArrow(x + 98, 407, i == 1 ? "↓" : "↑");
                }
                break;
            case "counterweight":
                GuideFloor(x);
                if (i < 2)
                {
                    float boardY = i == 0 ? 435 + phase * 45 : 480 - phase * 45;
                    float crateY = i == 0 ? 399 - phase * 70 : 329 + phase * 70;
                    GuideProp(0, x - 72, boardY, 82, 24);
                    if (i == 0)
                        GuideActor(x - 72, boardY, 103);
                    GuideProp(3, x + 65, crateY, 88, 88);
                    GuideArrow(x + 65, i == 0 ? 312 : 421, i == 0 ? "↑" : "↓");
                    if (i == 1)
                        GuideArrow(x - 72, 363, "↑");
                }
                else
                {
                    GuideProp(3, x + 35, 334 + phase * 40, 91, 91);
                    GuideActor(x - 100 + phase * 200, 500, 58, run);
                    GuideKey(x - 86, 329, "2");
                    GuideArrow(x + 88, 462, "→");
                }
                break;
            case "elevator":
                float liftY = i == 0 ? 480 - phase * 70 : i == 1 ? 445 : 410 + phase * 70;
                GuideProp(0, x, liftY, 158, 31);
                GuideActor(x, liftY, new[] { 64f, 90f, 108f }[i]);
                GuideArrow(x + 102, 416, i == 0 ? "↑" : i == 1 ? "=" : "↓");
                break;
            case "spring":
                GuideFloor(x);
                GuideProp(2, x, 470, 82, 32);
                GuideActor(x, 470 - bounce * new[] { 110f, 70f, 28f }[i], new[] { 58f, 82f, 110f }[i], 5);
                GuideArrow(x + 91, 395, "↑");
                break;
            case "bridge":
                Sprite(new Rect(x - 24, 470, 48, 59), Art.Cell("Fixtures", 2, 2, 0));
                float angle = (i - 1) * -10;
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, new Vector2(x, 470));
                GuideProp(0, x, 462, 228, 28);
                GuideProp(3, x + 77, 413, 50, 50);
                GUI.matrix = matrix;
                GuideActor(x - 76, 463 - Mathf.Sin(angle * Mathf.Deg2Rad) * 76, new[] { 61f, 86f, 115f }[i]);
                GuideArrow(x, 326, i == 0 ? "↑" : i == 1 ? "=" : "↓");
                break;
            case "checkpoint":
                GuideFloor(x);
                if (i == 1)
                {
                    GuideActor(x - 60, 485, 98);
                    Sprite(new Rect(x + 4, 358, 63, 63), Art.Cell("Icons", 2, 2, 0));
                    Sprite(new Rect(x + 58, 411, 63, 63), Art.Cell("Icons", 2, 2, 1));
                }
                else
                {
                    GuideProp(5, x + 53, 393, 46, 107);
                    GuideActor(x - 68 + phase * 68, 500, 97, run);
                    if (i == 2)
                        GuideKey(x, 322, "R");
                }
                break;
            case "gate":
                GuideFloor(x);
                Sprite(new Rect(x + 24, 383 - (i == 2 ? phase * 70 : 0), 32, 116), Art.Cell("CalibrationGate", 1, 1, 0));
                GuideActor(x - 63, 500, i == 0 ? 61 : 90, i == 2 ? run : 0);
                BodyBadge(new Rect(x - 61, 310, 122, 71), 1, true, teal);
                if (i == 0)
                {
                    GuideKey(x - 82, 390, "1");
                    GuideArrow(x + 85, 416, "≠");
                }
                else if (i == 1)
                {
                    Fill(new Rect(x - 108, 400 + phase * 95, 85, 3), teal);
                    ProgressSegments(new Rect(x - 105, 532, 210, 5), phase, teal);
                }
                else GuideArrow(x + 90, 429, "✓");
                break;
            case "capacitor":
                if (i < 2)
                {
                    Sprite(new Rect(x - 82, 455, 164, 53), Art.Cell("LabDevices", 2, 2, 0));
                    GuideActor(x + 23, 455 - (i == 0 ? bounce * 45 : 0), 105, i == 0 ? 5 : 0);
                    if (i == 0) GuideKey(x - 66, 318, "SPACE", 94);
                    else ProgressSegments(new Rect(x - 105, 320, 210, 12), phase, teal);
                    GuideArrow(x + 100, 399, i == 0 ? "↓" : "+");
                }
                else
                {
                    Sprite(new Rect(x - 110, 474, 220, 44), Art.Cell("LabDevices", 2, 2, 3));
                    GuideActor(x - 80 + phase * 160, 474, 59, run);
                    GuideKey(x - 74, 328, "2");
                    ProgressSegments(new Rect(x - 104, 391, 208, 10), 1 - phase, teal);
                }
                break;
            case "echo":
                GuideFloor(x);
                Sprite(new Rect(x - 107, 424, 110, 69), Art.Cell("AdvancedDevices", 2, 2, 0));
                if (i == 0)
                {
                    GuideActor(x - 46, 454, 118);
                    GuideKey(x + 66, 342, "E");
                    Label(new Rect(x + 31, 404, 71, 44), "L", 32, TextAnchor.MiddleCenter, teal);
                }
                else
                {
                    var savedColor = GUI.color; GUI.color = new Color(.2f, .8f, .8f, .5f);
                    GuideActor(x - 58, 454, 114); GUI.color = savedColor;
                    GuideActor(x + 25 + phase * 60, 500, 58, run);
                    if (i == 1) GuideKey(x + 69, 328, "2");
                    else Sprite(new Rect(x + 65, 377, 25, 120), Art.Cell("CalibrationGate", 1, 1, 0));
                    ProgressSegments(new Rect(x - 108, 517, 216, 8), 1-phase, teal);
                }
                break;
            case "shuttle":
                Fill(new Rect(x - 117, 493, 234, 5), copper);
                Fill(new Rect(x - 12, 506, 24, 5), teal);
                float carriage = x + (i == 0 ? phase * 100 - 50 : i == 2 ? 50-phase*100 : 0);
                Sprite(new Rect(carriage - 81, 459, 162, 40), Art.Cell("AdvancedDevices", 2, 2, 1));
                GuideActor(carriage, 459, new[] { 60f, 90f, 116f }[i]);
                GuideArrow(x + 95, 318, i == 0 ? "→" : i == 1 ? "=" : "←");
                if (i == 1) ProgressSegments(new Rect(x - 92, 525, 184, 6), phase, teal);
                break;
            case "prism":
                Sprite(new Rect(x - 35, 382, 75, 124), Art.Cell("AdvancedDevices", 2, 2, 2));
                GuideActor(x - 80, 500, new[] { 55f, 80f, 105f }[i]);
                for (int j = 0; j < 3; j++)
                {
                    Sprite(new Rect(x + 64, 329 + j*60, 38, 51), Art.Cell("AdvancedDevices", 2, 2, 3));
                    if (j == i) { Fill(new Rect(x + 32, 347+j*60, 37, 3), teal); GuideArrow(x + 109, 333+j*60, "✓"); }
                }
                Label(new Rect(x - 110, 312, 96, 40), new[] { "S", "M", "L" }[i], 30, TextAnchor.MiddleCenter, teal);
                break;
            case "relay":
                GuideFloor(x);
                Sprite(new Rect(x + 35, 391, 65, 108), Art.Cell("AdvancedDevices", 2, 2, 3));
                if (i == 0) BodyBadge(new Rect(x - 106, 339, 126, 72), 1, true, teal);
                else GuideActor(x - 50, 500, 87);
                if (i == 1) ProgressSegments(new Rect(x - 104, 327, 208, 10), phase, teal);
                if (i == 2) { GuideArrow(x + 76, 318, "✓"); GuideArrow(x - 60, 319, "→"); }
                break;
            case "airwell":
            case "freeflight":
                Sprite(new Rect(x - 90, 474, 180, 50), Art.Cell("LabDevices", 2, 2, 1));
                for (int a = 0; a < 5; a++)
                {
                    float ay = 370 + Mathf.Repeat(phase + a * .21f, 1) * 94;
                    Fill(new Rect(x - 72 + a * 36, ay, 2, 13), new Color(.2f, .65f, .65f, .45f));
                }
                float feet = id == "airwell" ? new[] { 377f, 420f, 473f }[i] : i == 0 ? 474 - phase * 100 : i == 1 ? 419 : 414 + phase * 49;
                if (id == "airwell")
                {
                    GuideProp(0, x + 94, feet, 65, 16);
                    GuideArrow(x + 87, feet - 59, "→");
                }
                GuideActor(x, feet, new[] { 57f, 78f, 99f }[i], 5);
                if (id == "freeflight") GuideArrow(x + 103, 385, i == 0 ? "↑" : i == 1 ? "=" : "↓");
                break;
        }
    }
}
