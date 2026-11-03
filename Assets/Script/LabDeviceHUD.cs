using UnityEngine;

public partial class GameHUD
{
    void BodyBadge(Rect r, int size, bool active, Color accent)
    {
        var tint = GUI.color;
        GUI.color = active ? new Color(.74f, 1f, .93f) : new Color(.86f, .88f, .84f, .75f);
        Card(r, 1);
        GUI.color = tint;
        float h = 28 + size * 13;
        Sprite(new Rect(r.x + 9, r.y + 9 + (54 - h), 42, h), Art.Cell("Subject", 4, 2, 0, false, 300));
        Label(new Rect(r.x + 55, r.y + 5, r.width - 63, 49), new[] { "S", "M", "L" }[size], 34, TextAnchor.MiddleCenter, active ? accent : muted);
        Fill(new Rect(r.x + 15, r.yMax - 9, r.width - 30, 3), active ? accent : new Color(.68f, .72f, .69f));
    }
    void ProgressSegments(Rect r, float progress, Color color)
    {
        for (int i = 0; i < 16; i++)
            Fill(new Rect(r.x + i * r.width / 16, r.y, r.width / 16 - 3, r.height), i < Mathf.CeilToInt(progress * 16) ? color : new Color(.76f, .80f, .75f));
    }
    void DeviceOverlay()
    {
        // Instructions live on the walls. This small readout is only live device feedback.
        if (EnvironmentalGuidance.Instance && EnvironmentalGuidance.Instance.View && EnvironmentalGuidance.Instance.View.Visible) return;
        var p=flow.Player;var camera=Camera.main;if(!p || !camera)return;
        LabDevice nearest=null;float distance=3.4f;
        foreach(var device in LabDevice.Active)
        {
            if(!device)continue;
            float d=Vector2.Distance(p.GetComponent<Collider2D>().bounds.center,device.transform.position);
            if(device is VectorAirwell w && w.Occupied)d=0;
            if(device is KineticCapacitor k && k.OnBridge)d=0;
            if(d<distance){distance=d;nearest=device;}
        }
        if(!nearest)return;
        string status="";float progress=-1;
        if(nearest is MassGate gate){status="TARGET "+new[]{"S","M","L"}[gate.requiredSize]+" / "+gate.Status;progress=gate.Open?1:gate.Progress;}
        else if(nearest is KineticCapacitor core){status=core.Status;progress=core.Energy/core.lifetime;}
        else if(nearest is MassEcho echo){status=echo.Status;progress=echo.Remaining/echo.duration;}
        else if(nearest is RailShuttle rail){status=rail.Status;progress=rail.Docked?1:rail.DockProgress;}
        else if(nearest is MassPrism prism){status=prism.Status;progress=(prism.Charge[0]+prism.Charge[1]+prism.Charge[2])/3;}
        else if(nearest is BodyRelay relay){status=relay.Status;progress=relay.Progress;}
        else if(nearest is MassLockStation){var hold=p.GetComponent<MassLock>();status=hold && hold.Locked?"SHAPE HELD / Q TO RELEASE":"MASS LOCK / E";progress=hold && hold.Locked?hold.Remaining/MassLock.Duration:-1;}
        else if(nearest is VectorAirwell air){if(!air.Occupied)return;status=new[]{"SMALL","MEDIUM","HEAVY"}[p.Size]+" / "+(air.tiered?new[]{"HIGH ROUTE","MIDDLE ROUTE","LOW ROUTE"}[p.Size]:new[]{"RISING","HOLDING HEIGHT","DESCENDING"}[p.Size]);}
        var point=camera.WorldToScreenPoint(nearest.Anchor);
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        float x=(point.x-(Screen.width-1600*scale)*.5f)/scale;
        float y=(Screen.height-point.y-(Screen.height-900*scale)*.5f)/scale;
        Rect panel=new Rect(Mathf.Clamp(x-150,30,1270),Mathf.Clamp(y-42,150,640),300,progress<0?49:65);
        var body=p.GetComponent<Collider2D>().bounds;var lo=camera.WorldToScreenPoint(body.min);var hi=camera.WorldToScreenPoint(body.max);
        var subject=new Rect((lo.x-(Screen.width-1600*scale)*.5f)/scale-20,(Screen.height-hi.y-(Screen.height-900*scale)*.5f)/scale-20,(hi.x-lo.x)/scale+40,(hi.y-lo.y)/scale+40);
        if(panel.Overlaps(subject))panel.x=subject.xMax+panel.width+24<1570?subject.xMax+24:Mathf.Max(30,subject.xMin-panel.width-24);
        if(FirstEncounterGuide.Instance && FirstEncounterGuide.Instance.LocalVisible && panel.Overlaps(FirstEncounterGuide.Instance.Panel))panel=BesideEncounter(new Rect(650,225,300,panel.height));
        Card(panel,1);Label(new Rect(panel.x+24,panel.y+9,252,30),status,16,TextAnchor.MiddleCenter,teal);
        if(progress>=0)ProgressSegments(new Rect(panel.x+25,panel.y+46,250,5),progress,teal);
    }
    void ControlReference()
    {
        string[] keys = { "A D", "SPACE", "F", "1", "2", "E", "Q", "R", "ESC" };
        string[] actions = { "MOVE", "JUMP / HOLD HIGHER", "COLLECT", "EAT / GROW", "EAT / SHRINK", "USE ECHO / MASS LOCK", "RELEASE MASS LOCK", "RETRY CHECKPOINT", "PAUSE / RESUME" };
        for (int i = 0; i < keys.Length; i++)
        {
            float y = 268 + i * 37;
            Card(new Rect(809, y, 98, 34), 1);
            Label(new Rect(821, y + 2, 74, 30), keys[i], 17, TextAnchor.MiddleCenter);
            Sprite(new Rect(921, y + 2, 30, 30), i == 2 || i == 3 || i == 4 ? Art.Cell("Icons", 2, 2, i == 4 ? 1 : 0) : Art.Cell("Subject", 4, 2, i == 1 ? 5 : 0, false, 300));
            Label(new Rect(969, y + 1, 290, 32), actions[i], 18);
        }
    }
}
