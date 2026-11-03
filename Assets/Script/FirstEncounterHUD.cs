using System.Linq;
using UnityEngine;

public partial class GameHUD
{
    void EncounterLabel(Rect rect,string text,int size,TextAnchor anchor=TextAnchor.MiddleLeft,Color? color=null)
    {
        while(size>16 && Style(size,color??ink,anchor).CalcHeight(new GUIContent(text),rect.width)>rect.height)size--;
        Label(rect,text,size,anchor,color);
    }
    void EncounterKey(Rect rect,string keys)
    {
        if(string.IsNullOrWhiteSpace(keys))return;
        var parts=keys.Split('/');
        float w=parts.Length>1?34:keys=="SPACE"?82:36;
        float total=parts.Length*w+(parts.Length-1)*7;
        for(int i=0;i<parts.Length;i++)
        {
            var key=new Rect(rect.center.x-total*.5f+i*(w+7),rect.y,w,32);
            Card(key,1);Label(key,parts[i].Trim(),17,TextAnchor.MiddleCenter);
        }
    }
    void FirstEncounterOverlay()
    {
        var guide=FirstEncounterGuide.Instance;
        if(!guide || !guide.LocalVisible || guide.Local==null)return;
        var item=guide.Local;var lesson=item.Lesson;var r=guide.Panel;
        if(guide.Compact){CompactEncounter(guide,item,r);return;}
        Card(r,1);
        Label(new Rect(r.x+44,r.y+18,442,22),"FIRST ENCOUNTER / CHAPTER "+item.Chapter.ToString("00"),14,TextAnchor.MiddleLeft,copper);
        EncounterLabel(new Rect(r.x+44,r.y+46,442,34),lesson.Title,25);
        EncounterLabel(new Rect(r.x+44,r.y+86,442,60),lesson.Purpose,20);
        for(int i=0;i<3;i++)
        {
            float x=r.x+44+i*149;
            Fill(new Rect(x,r.y+151,144,85),new Color(.85f,.9f,.83f,.45f));
            EncounterArt(new Rect(x,r.y+148,144,88),item,i);
            if(i<lesson.Keys.Length)EncounterKey(new Rect(x,r.y+237,144,32),lesson.Keys[i]);
            EncounterLabel(new Rect(x+2,r.y+273,140,50),lesson.Steps[i],17,TextAnchor.MiddleCenter,teal);
        }
        Fill(new Rect(r.x+44,r.y+324,442,1),new Color(.45f,.6f,.56f,.45f));
        EncounterLabel(new Rect(r.x+44,r.y+329,442,52),item.Relation,18,TextAnchor.MiddleLeft,teal);
        if(Button(new Rect(r.x+142,r.y+382,246,40),guide.CanAcknowledge?"GOT IT / TAB":"TAKE A MOMENT TO READ",false,guide.CanAcknowledge))guide.Acknowledge();
    }
    void CompactEncounter(FirstEncounterGuide guide,FirstEncounterGuide.Entry item,Rect r)
    {
        var lesson=item.Lesson;int page=guide.Page;
        bool wide=r.width>400;float w=r.width-56;
        float purpose=wide?72:88,art=wide?141:155,relation=wide?235:266,bottom=wide?301:331;
        Card(r,1);
        Label(new Rect(r.x+28,r.y+12,w,22),"FIRST ENCOUNTER / "+(page+1)+" OF 3",13,TextAnchor.MiddleLeft,copper);
        EncounterLabel(new Rect(r.x+28,r.y+36,w,wide?34:49),lesson.Title,22);
        EncounterLabel(new Rect(r.x+28,r.y+purpose,w,wide?62:66),lesson.Purpose,18);
        Fill(new Rect(r.x+28,r.y+art+2,142,wide?86:100),new Color(.85f,.9f,.83f,.45f));
        EncounterArt(new Rect(r.x+28,r.y+art,142,wide?88:102),item,page);
        EncounterLabel(new Rect(r.x+179,r.y+art+4,r.width-207,52),lesson.Steps[page],19,TextAnchor.MiddleCenter,teal);
        if(page<lesson.Keys.Length)EncounterKey(new Rect(r.x+179,r.y+art+(wide?56:64),r.width-207,32),lesson.Keys[page]);
        EncounterLabel(new Rect(r.x+28,r.y+relation,w,62),item.Relation,17,TextAnchor.MiddleLeft,teal);
        if(Button(new Rect(r.x+28,r.y+bottom,92,40),"←",false,page>0))guide.Previous();
        bool next=page<2;
        if(Button(new Rect(r.x+130,r.y+bottom,r.width-158,40),next?"NEXT / TAB":guide.CanAcknowledge?"GOT IT / TAB":"READ FIRST",false,next||guide.CanAcknowledge))guide.Advance();
    }
    Rect BesideEncounter(Rect preferred)
    {
        var guide=FirstEncounterGuide.Instance;
        if(!guide || !guide.LocalVisible || !preferred.Overlaps(guide.Panel))return preferred;
        foreach(float x in new[]{40f,600f,1160f})
        {
            var candidate=new Rect(x,preferred.y,Mathf.Min(preferred.width,400),preferred.height);
            if(!candidate.Overlaps(guide.Panel))return candidate;
        }
        return preferred;
    }
    void Discovery()
    {
        var guide=FirstEncounterGuide.Instance;if(!guide || guide.Global==null)return;
        var item=guide.Global;var page=item.Lesson;
        if(flow.Player)HUD(false);else Background();
        Veil(.65f);Card(new Rect(210,115,1180,670));
        Label(new Rect(300,137,1000,30),"NEW CHAPTER RULE / FIRST TIME ONLY",16,TextAnchor.MiddleCenter,copper);
        EncounterLabel(new Rect(300,176,1000,60),page.Title,37,TextAnchor.MiddleCenter);
        EncounterLabel(new Rect(350,241,900,60),page.Purpose,23,TextAnchor.MiddleCenter,teal);
        for(int i=0;i<3;i++)
        {
            var r=new Rect(290+i*350,318,320,282);Card(r,1);
            EncounterArt(new Rect(r.x+10,r.y+14,300,190),item,i);
            if(i<page.Keys.Length)EncounterKey(new Rect(r.x+30,r.y+189,260,32),page.Keys[i]);
            EncounterLabel(new Rect(r.x+25,r.y+226,270,48),page.Steps[i],21,TextAnchor.MiddleCenter);
            if(i<2)Label(new Rect(r.xMax,412,30,42),"→",27,TextAnchor.MiddleCenter,teal);
        }
        var circuit=UnityEngine.Object.FindAnyObjectByType<RouteCircuit>();
        string extra=circuit && circuit.InterlockSources.Any(s=>s is RailShuttle)?" Required cyan docks also count.":"";
        if(circuit && circuit.InterlockSources.Any(s=>s is MassPrism))extra+=" All optical receivers also count.";
        string note=page.Id=="chapter_circuit"?"R numbers identify route nodes."+extra+" The HUD names the next missing item. Match its R label on the map; this counter does not count floors.":
            "S lights TOP, M lights MIDDLE, L lights LOW. Finish each receiver before changing shape. Matching P codes show the locks this circuit powers.";
        EncounterLabel(new Rect(330,617,940,70),note,22,TextAnchor.MiddleCenter,teal);
        if(Button(new Rect(570,706,460,52),guide.GlobalSeconds>=1.2f?"START / ENTER":"TAKE A MOMENT TO READ",true,guide.GlobalSeconds>=1.2f))guide.AcknowledgeGlobal();
    }
    void EncounterArt(Rect box,FirstEncounterGuide.Entry item,int step)
    {
        var old=GUI.matrix;
        float scale=Mathf.Min(box.width/300f,box.height/230f);
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(box.center.x-150*scale,box.y-300*scale),Quaternion.identity,new Vector3(scale,scale,1));
        float phase=PlayerPrefs.GetInt("Fit.ReducedMotion",0)==1?.5f:Mathf.Repeat(Time.unscaledTime*.5f,1);
        string id=item.Lesson.Id;float x=150;
        if(id=="chapter_circuit")
        {
            if(step==0)
            {
                var first=UnityEngine.Object.FindAnyObjectByType<RouteCircuit>()?.Links.FirstOrDefault();
                int size=first?first.requiredSize:1;
                Sprite(new Rect(175,375,65,115),Art.Cell("AdvancedDevices",2,2,3));
                BodyBadge(new Rect(35,325,122,71),size,true,teal);GuideActor(95,500,new[]{58f,84f,112f}[size]);
                Label(new Rect(180,325,90,35),"R1",25,TextAnchor.MiddleCenter,teal);
            }
            else if(step==1)
            {
                Card(new Rect(24,345,252,145),1);
                Label(new Rect(43,358,214,35),"EXIT LINKS",26,TextAnchor.MiddleCenter);
                ProgressSegments(new Rect(45,414,210,12),phase,teal);
                Label(new Rect(45,438,210,34),"R1  ·  R2  ·  R3",22,TextAnchor.MiddleCenter,teal);
            }
            else {GuideProp(4,200,350-phase*18,70,150);GuideActor(80+phase*50,500,75,2);GuideArrow(150,330,"→");}
        }
        else if(id=="optics_circuit")
        {
            if(step==0)GuidePicture("prism",(int)(phase*3)%3,x,phase);
            else if(step==1)
            {
                for(int i=0;i<3;i++)
                {
                    Sprite(new Rect(55+i*78,365,65,105),Art.Cell("AdvancedDevices",2,2,3));
                    Fill(new Rect(55+i*78,484,61,9),teal);
                    Label(new Rect(55+i*78,327,65,30),new[]{"TOP","MID","LOW"}[i],19,TextAnchor.MiddleCenter,teal);
                }
            }
            else
            {
                GuideProp(4,205,355,62,142);GuideProp(0,85,455-phase*35,105,25);GuideActor(85,455-phase*35,68);
                Label(new Rect(30,318,240,38),"P1 / POWER READY",23,TextAnchor.MiddleCenter,teal);
            }
        }
        else if(id=="echo")
        {
            GuideFloor(x);Sprite(new Rect(45,444,140,52),Art.Cell("AdvancedDevices",2,2,0));
            if(step<2)GuideActor(114,451,116);
            if(step==1){GuideArrow(220,373,"→");GuideProp(4,245,369,30,122);}
            if(step==2)
            {
                var c=GUI.color;GUI.color=new Color(.3f,1,.9f,.45f);GuideActor(105,451,116);GUI.color=c;
                GuideActor(165+phase*55,500,58,2);GuideProp(4,260,351-phase*22,35,128);
                ProgressSegments(new Rect(35,521,230,8),1-phase,teal);
            }
        }
        else if(id=="capacitor")
        {
            GuideFloor(x);
            if(step<2)
            {
                Sprite(new Rect(60,448,180,50),Art.Cell("LabDevices",2,2,0));
                GuideActor(150,450-(step==1?Mathf.Sin(phase*Mathf.PI)*65:0),110,step==1?5:0);
                if(step==1)GuideArrow(250,372,"↓");
            }
            else
            {
                Sprite(new Rect(25,476,250,40),Art.Cell("LabDevices",2,2,3));GuideActor(65+phase*160,476,58,2);
                ProgressSegments(new Rect(35,330,230,8),1-phase,teal);
            }
        }
        else if(id=="relay" || id=="gate")
        {
            int size=item.Owner is BodyRelay relay?relay.requiredSize:item.Owner is MassGate gate?gate.requiredSize:1;
            BodyBadge(new Rect(35,315,122,71),size,true,teal);
            GuideActor(95,500,new[]{58f,84f,112f}[step==0?(size+1)%3:size]);
            if(id=="relay")Sprite(new Rect(184,384,62,116),Art.Cell("AdvancedDevices",2,2,3));
            else Sprite(new Rect(190,351-(step==2?phase*36:0),45,149),Art.Cell("CalibrationGate",1,1,0));
            if(step==1)ProgressSegments(new Rect(35,517,230,9),phase,teal);
            if(step==2)GuideArrow(220,316,"✓");
        }
        else if(id=="linkeddoor" || id=="exit")
        {
            if(step==0)
            {
                GuideProp(4,180,350,72,150);
                Label(new Rect(25,318,250,35),item.Owner is SignalBarrier b?MechanismConnections.Code(b.Source)+" / LOCKED":"EXIT / LOCKED",23,TextAnchor.MiddleCenter,copper);
            }
            else if(step==1)
            {
                var source=item.Owner is SignalBarrier door?door.Source:UnityEngine.Object.FindAnyObjectByType<RouteCircuit>()?.NextSource;
                Sprite(new Rect(55,360,190,140),source is MassEcho?Art.Cell("AdvancedDevices",2,2,0):source is MassPrism?Art.Cell("AdvancedDevices",2,2,2):source is RailShuttle?Art.Cell("AdvancedDevices",2,2,1):Art.Cell("AdvancedDevices",2,2,3));
                Label(new Rect(25,318,250,35),MechanismConnections.Code(source),25,TextAnchor.MiddleCenter,teal);
            }
            else {GuideProp(4,210,350-phase*25,65,150);GuideActor(80+phase*70,500,75,2);GuideArrow(150,325,"→");}
        }
        else if(id=="bridge_reverse")
        {
            GuideFloor(x);GuideProp(0,x,468,240,25);GuideProp(3,75,416,52,52);
            GuideActor(230,468,new[]{58f,84f,112f}[step]);Sprite(new Rect(130,491,40,35),Art.Cell("Fixtures",2,2,0));
            GuideArrow(235,325,step==1?"=":step==0?"↑":"↓");
        }
        else GuidePicture(id=="shuttle_return"?"shuttle":id,id=="shuttle_return"?new[]{0,2,1}[step]:step,x,phase);
        GUI.matrix=old;
    }
    bool LockedRouteFeedback()
    {
        if(!flow.Player || !Camera.main)return false;
        var p=flow.Player;
        var door=UnityEngine.Object.FindObjectsByType<SignalBarrier>().Where(d=>!d.Open && Mathf.Abs(d.transform.position.x-p.transform.position.x)<3.5f && Mathf.Abs(d.transform.position.y-p.transform.position.y)<3.8f).OrderBy(d=>(d.transform.position-p.transform.position).sqrMagnitude).FirstOrDefault();
        Component source=null;string title="",reason="";
        if(door)
        {
            title="LOCKED / "+MechanismConnections.Code(door.Source);reason=door.Requirement;source=door.Source is RouteCircuit c?c.NextSource:door.Source;
        }
        else
        {
            var exit=UnityEngine.Object.FindObjectsByType<LevelGoal>().FirstOrDefault(g=>Vector2.Distance(g.transform.position,p.transform.position)<3.7f);
            var circuit=UnityEngine.Object.FindAnyObjectByType<RouteCircuit>();
            if(exit && circuit && !circuit.Complete){title="EXIT LOCKED";reason=circuit.MissingReason;source=circuit.NextSource;}
        }
        if(title.Length==0)return false;
        bool teaching=FirstEncounterGuide.Instance && FirstEncounterGuide.Instance.LocalVisible;
        var r=teaching?BesideEncounter(new Rect(600,137,400,88)):new Rect(485,137,630,72);Card(r,1);
        Label(new Rect(r.x+22,r.y+7,r.width-44,23),title,17,TextAnchor.MiddleCenter,copper);
        EncounterLabel(new Rect(r.x+22,r.y+31,r.width-44,r.height-38),reason+(source?"  "+MechanismConnections.Code(source)+" / "+MechanismConnections.Direction(p.transform.position,source.transform.position):""),18,TextAnchor.MiddleCenter);
        if(!teaching && source && !(source is RouteCircuit))
        {
            var screen=FirstEncounterGuide.Project(new Bounds(source.transform.position,Vector3.one));
            if(screen.center.x<70 || screen.center.x>1530 || screen.center.y<145 || screen.center.y>700)
            {
                float x=source.transform.position.x<p.transform.position.x?30:1450;
                Card(new Rect(x,240,120,76),1);
                Label(new Rect(x+10,247,100,25),MechanismConnections.Code(source),19,TextAnchor.MiddleCenter,teal);
                Label(new Rect(x+10,276,100,28),MechanismConnections.Direction(p.transform.position,source.transform.position),17,TextAnchor.MiddleCenter);
            }
        }
        return true;
    }
}
