using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Painted field marks: real sprites, sketched arrows, keycaps and one short action.</summary>
public class WorldGuideView : MonoBehaviour
{
    public bool TextFits
    {
        get { foreach (var t in GetComponentsInChildren<Text>()) if(t.preferredHeight>t.rectTransform.rect.height+2) return false; return true; }
    }
    public bool Compact { get; private set; }
    public int Phase { get; set; }
    public int DisplayedStep { get; private set; }
    public string Caption => caption ? caption.text : "";
    public bool Visible => group && group.alpha>.15f;
    CanvasGroup group;
    RectTransform content;
    Image actor, second, mechanism, icon;
    Text title, caption, badge, secondBadge;
    string lastId;
    int lastStep = -1;
    int lastSize = -1;
    bool success;
    float feet;
    static Font font;
    static readonly Color Chalk = new Color(.97f,.94f,.79f), Cyan = new Color(.46f,.96f,.88f);
    public void Build(Rect world, bool wall, bool compact, int stage)
    {
        Compact = compact;
        var root = GetComponent<RectTransform>(); if (!root) root = gameObject.AddComponent<RectTransform>();
        var canvas = GetComponent<Canvas>(); if (!canvas) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.overrideSorting = true; canvas.sortingOrder = wall ? 8 : -12;
        root.sizeDelta = new Vector2(560,compact?190:260);
        root.localScale = Vector3.one * (compact ? .008f : .01f);
        root.position = new Vector3(world.center.x,world.center.y,wall?-.05f:.2f);
        group = gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
        var wash = Element("Painted wall wash",new Rect(0,0,560,compact?190:260),root).gameObject.AddComponent<ChalkWash>();
        wash.color = new Color(.035f,.13f,.145f,.94f); wash.raycastTarget = false;
        Stroke(root,new[]{new Vector2(15,-31),new Vector2(180,-30),new Vector2(271,-32)},new Color(.7f,.85f,.77f,.35f),1.4f);
        TextAt(root,new Rect(20,9,520,19),CampaignMap.Regions[CampaignMap.Region(stage)]+" / FIELD MARKS",14,new Color(.62f,.82f,.75f));
        content=Element("Action sketch",new Rect(0,0,560,compact?190:260),root);
    }
    public void SetOpacity(float opacity) { if(group) group.alpha=opacity; }
    public void SetSuccess() { success = true; if(caption) { caption.text="GOT IT. KEEP GOING.";caption.color=Cyan; } }
    public void Animate(string id,int step,Component owner,PlayerController p,float age)
    {
        DisplayedStep=step;
        if (id != lastId || step != lastStep || p.Size != lastSize) { Draw(id,step,owner,p);lastId=id;lastStep=step;lastSize=p.Size; }
        bool reduced=PlayerPrefs.GetInt("Fit.ReducedMotion",0)==1;
        float phase=reduced?.5f:Mathf.Repeat(age*.45f,1), bounce=Mathf.Sin(phase*Mathf.PI);
        if (actor)
        {
            float x=120, h=82, y=feet;
            actor.enabled = !(id=="pressure" && step==1);
            if(id=="movement") x=68+phase*150;
            if(id=="jump") { x=78+phase*200;h=62;y-=bounce*(Compact?24:46); }
            if(id=="spring") { h=54;y-=bounce*(Compact?24:54);if(step==1 && phase>.55f)x+=(phase-.55f)*140; }
            if(id=="capacitor") { h=90;y-=bounce*18; }
            if(id=="duct" || id=="food") x=70+phase*150;
            if(id=="duct")h=54;
            if(id=="pressure")
            {
                h=step==0?96:62;y=step==0?feet-8:step==1?feet-(Compact?30:47)+phase*(Compact?18:35):feet-12-phase*(Compact?18:35);
                SetBox(mechanism,step==0?74:75,y,step==0?96:155,Compact?14:step==0?18:22);
            }
            if(id=="elevator")
            {
                h=(Compact?new[]{54f,76f,84f}:new[]{54f,78f,90f})[step];y=step==0?feet-12-phase*(Compact?18:35):step==1?feet-(Compact?18:28):feet-(Compact?20:30)+phase*(Compact?12:18);
                SetBox(mechanism,75,y,155,Compact?14:22);
            }
            if(id=="counterweight")
            {
                h=p.Size==1?78:96;y=feet-8;
                float motion=p.Size==1?.5f:step==0?phase:1-phase;
                float crate=Compact?40:70;SetBox(mechanism,244,feet-crate-motion*(Compact?12:32),crate,crate);
            }
            if(id=="bridge") { h=78;y=feet-16;if(owner is BalanceBridge balance && balance.riderSide>0)x=286; }
            if(id=="shuttle")
            {
                x=step==0?80+phase*140:step==1?180:180+phase*95;
                h=step==1?78:54;y=feet-(Compact?23:30);SetBox(mechanism,x-70,y,140,Compact?20:30);
                if(owner is RailShuttle returnRail && returnRail.requireReturn && Phase==2 && step==1)
                {x=270-phase*150;h=96;SetBox(mechanism,x-70,y,140,Compact?20:30);}
            }
            if(id=="echo")
            {
                x=step==2?176+phase*55:96;h=step==2?54:96;y=step==2?feet:feet-12;
                if(second)FitActor(second,96,feet-12,96,false,age);
            }
            if(id=="metabolism" || id=="ambient" || id=="heat" || id=="cold")
            {
                float rest=id=="heat"?Mathf.Repeat(phase*1.8f,1):id=="cold"?phase*.45f:phase;
                float move=id=="cold"?Mathf.Repeat(phase*1.8f,1):id=="heat"?phase*.45f:phase;
                h=50+rest*34;x=112;
                FitActor(second,287,feet,84-move*34,true,age);
            }
            else if(id=="airwell" || id=="freeflight")
            { h=new[]{54f,78f,90f}[step];y=step==0?feet-phase*34:step==1?feet-18:feet-18+phase*18; }
            else if(id=="prism")h=new[]{54f,78f,104f}[step];
            else if(id=="gate" && owner is MassGate gate)h=new[]{54f,78f,104f}[gate.requiredSize];
            else if(id=="relay" && owner is BodyRelay relay)h=new[]{54f,78f,104f}[relay.requiredSize];
            FitActor(actor,x,y,h,id=="movement" || id=="food" || id=="duct",age);
        }
        if(success && caption) { caption.text="GOT IT. KEEP GOING."; caption.color=Cyan; }
    }
    void Draw(string id,int step,Component owner,PlayerController p)
    {
        for(int i=content.childCount-1;i>=0;i--) { content.GetChild(i).gameObject.SetActive(false); Destroy(content.GetChild(i).gameObject); }
        actor=second=mechanism=icon=null;badge=secondBadge=null;
        string heading, line, key;
        Describe(id,step,owner,p,out heading,out line,out key);
        title=TextAt(content,new Rect(20,35,520,Compact?32:54),heading,Compact?24:28,Chalk);
        feet=Compact?145:210;
        float artTop=Compact?83:102;
        caption=TextAt(content,new Rect(20,Compact?152:220,520,36),line,Compact?25:23,Chalk);
        actor=Picture(content,"Demonstration body",new Rect(80,artTop,80,98),Art.Cell("Subject",4,2,0,false,300));
        if(id=="jump")
        {
            Stroke(content,new[]{new Vector2(32,-feet),new Vector2(141,-feet)},Chalk,2);
            Stroke(content,new[]{new Vector2(239,-feet),new Vector2(353,-feet)},Chalk,2);
        }
        else Stroke(content,new[]{new Vector2(32,-feet),new Vector2(153,-feet+1),new Vector2(355,-feet-1)},new Color(.78f,.86f,.74f,.65f),2);
        if(!string.IsNullOrEmpty(key)) Keycap(content,key,420,Compact?89:116);
        else TextAt(content,new Rect(407,Compact?90:113,127,65),id=="heat"?"HEAT":id=="cold"?"COLD":"TRY IT",22,Cyan);
        if(id=="movement") { Arrow(40,feet-45,-25,0);Arrow(266,feet-45,32,0); }
        else if(id=="jump") { Arrow(233,feet-12,0,-65); }
        else if(id=="metabolism" || id=="ambient" || id=="heat" || id=="cold")
        {
            second=Picture(content,"Moving body",new Rect(247,artTop,80,98),Art.Cell("Subject",4,2,1,false,300));
            badge=TextAt(content,new Rect(54,artTop-4,128,22),id=="heat"?"REST / FASTER":id=="cold"?"REST / SLOWER":"REST / GROW",16,Cyan);
            secondBadge=TextAt(content,new Rect(220,artTop-4,158,22),id=="cold"?"MOVE / FASTER":id=="heat"?"MOVE / SLOWER":"MOVE / SHRINK",16,Cyan);
            if(Compact) { badge.rectTransform.anchoredPosition=new Vector2(44,-77);secondBadge.rectTransform.anchoredPosition=new Vector2(225,-77); }
            else { Arrow(177,feet-21,0,-40);Arrow(352,feet-63,0,40); }
        }
        else if(id=="food" || id=="duct")
        {
            icon=Picture(content,"Food to try",new Rect(238,artTop+(Compact?10:23),Compact?45:65,Compact?45:65),Art.Cell("Icons",2,2,id=="food" && owner is FoodVisual food ? (food.fresh?1:0) : 1));
            Arrow(225,feet-55,-40,0);
            if(id=="duct") Picture(content,"Low passage",new Rect(171,artTop+(Compact?9:28),150,Compact?16:23),Art.Cell("Props",3,2,0));
        }
        else if(id=="score") { TextAt(content,new Rect(218,Compact?88:artTop+18,150,Compact?56:64),"TIME\nBONUS",Compact?18:22,Cyan);Arrow(292,feet-8,36,-28); }
        else if(id=="bridge")
        {
            mechanism=Picture(content,"Balance deck",new Rect(58,feet-16,285,18),Art.Cell("Props",3,2,0));
            Picture(content,"Equal-mass crate",new Rect(owner is BalanceBridge balance && balance.riderSide>0?82:274,feet-61,45,45),Art.Cell("Props",3,2,3));
            Picture(content,"Bridge pivot",new Rect(185,feet-2,30,Compact?7:11),Art.Cell("Fixtures",2,2,0));
            TextAt(content,new Rect(owner is BalanceBridge other && other.riderSide>0?153:225,artTop-2,130,23),"BALANCE",16,Cyan);
        }
        else if(id=="echo")
        {
            mechanism=Picture(content,"Recording pad",new Rect(43,feet-44,110,45),Art.Cell("AdvancedDevices",2,2,0));
            if(step==2){second=Picture(content,"Recorded body",new Rect(50,110,78,96),Art.Cell("Subject",4,2,0,false,300));second.color=new Color(.35f,1,.9f,.45f);}
            Arrow(200,feet-45,55,0);
        }
        else if(id=="shuttle")
        {
            mechanism=Picture(content,"Steerable carriage",new Rect(60,feet-30,140,30),Art.Cell("AdvancedDevices",2,2,1));
            Stroke(content,new[]{new Vector2(166,-feet-3),new Vector2(193,-feet-3)},Cyan,4);
            Arrow(290,feet-70,35,0);
        }
        else
        {
            Sprite sprite = id=="spring"?Art.Cell("Props",3,2,2):id=="pressure"?Art.Cell("Props",3,2,step==0?1:0):id=="counterweight"?Art.Cell("Props",3,2,3):id=="checkpoint"?Art.Cell("Props",3,2,5):id=="gate"?Art.Cell("CalibrationGate",1,1,0):id=="capacitor"?Art.Cell("LabDevices",2,2,0):id=="airwell"||id=="freeflight"?Art.Cell("LabDevices",2,2,1):id=="echo"?Art.Cell("AdvancedDevices",2,2,0):id=="shuttle"?Art.Cell("AdvancedDevices",2,2,1):id=="prism"?Art.Cell("AdvancedDevices",2,2,2):id=="relay"?Art.Cell("AdvancedDevices",2,2,3):id=="masslock"?Art.Cell("MassLockStation",1,1,0):Art.Cell("Props",3,2,0);
            if(id=="pressure" && owner is ButtonController unpowered && unpowered.PowerReady!=null && !unpowered.PowerReady())sprite=Art.Cell("AdvancedDevices",2,2,2);
            mechanism=Picture(content,"Relevant mechanism",new Rect(220,artTop+10,133,Compact?58:97),sprite);
            if(id=="counterweight")Picture(content,"Pressure board",new Rect(74,feet-8,96,Compact?14:18),Art.Cell("Props",3,2,0));
            if(id=="elevator" || id=="airwell" || id=="freeflight")Arrow(351,feet-31,0,step==1?0:step==0?-46:46);
            else Arrow(178,feet-42,38,0);
            if(id=="gate" || id=="relay")TextAt(content,new Rect(220,artTop-6,133,23),new[]{"SMALL","MEDIUM","HEAVY"}[id=="gate" && owner is MassGate g?g.requiredSize:id=="relay" && owner is BodyRelay r?r.requiredSize:1],16,Cyan);
        }
    }
    static void SetBox(Image image,float x,float y,float width,float height)
    {if(!image)return;image.rectTransform.anchoredPosition=new Vector2(x,-y);image.rectTransform.sizeDelta=new Vector2(width,height);}
    void FitActor(Image image,float center,float bottom,float height,bool running,float time)
    {
        if(!image)return;
        if(Compact)height*=lastId=="heat" || lastId=="cold" || lastId=="ambient" ? .42f : .65f;
        var rect=image.rectTransform;rect.sizeDelta=new Vector2(height*.8f,height);rect.anchoredPosition=new Vector2(center-height*.4f,-bottom+height);
        int frame=running && PlayerPrefs.GetInt("Fit.ReducedMotion",0)==0?1+(int)(time*7)%3:lastId=="jump" || lastId=="spring" || lastId=="capacitor"?5:0;
        image.sprite=Art.Cell("Subject",4,2,frame,false,300);
    }
    void Describe(string id,int step,Component owner,PlayerController p,out string title,out string caption,out string key)
    {
        key="";
        switch(id)
        {
            case "movement":title="MAKE YOUR MOVE";caption="Walk either way.";key="A / D";return;
            case "jump":title="MIND THE GAP";caption="Hold for a higher jump.";key="SPACE";return;
            case "food":title="TAKE IT WITH YOU";caption="Collect now. Eat when needed.";key="F";return;
            case "duct":title="MAKE ROOM TO PASS";caption="Eat fresh food to get smaller.";key="2";return;
            case "metabolism":case "ambient":title="PACE CHANGES MASS";caption="Move: lighter. Rest: heavier.";return;
            case "heat":title="HEAT CHANGES THE RHYTHM";caption="Rest: gain faster. Move: lose slower.";return;
            case "cold":title="COLD CHANGES THE RHYTHM";caption="Move: lose faster. Rest: gain slower.";return;
            case "pressure":title="CALL THE LIFT";caption=new[]{"Get heavy. Press the plate.","Wait for the lift to arrive.","Step aboard and ride up."}[step];key=step==0?"1":step==2?"A / D":"";
                if(owner is ButtonController button && button.PowerReady!=null && !button.PowerReady()){title="POWER THE LIFT";caption="Light all three receivers first.";key="1 / 2";}return;
            case "counterweight":title="OPEN IT. THEN RACE IT.";caption=step==1 && p.Size==1?"Medium holds this height.":new[]{"Get heavy to raise the stone.","Step off: the stone comes down.","Get small and run through."}[step];key=step==0?"1":step==2?"2":"";return;
            case "elevator":title="LET YOUR BODY SET THE HEIGHT";caption=new[]{"Small rises.","Medium holds still.","Heavy descends."}[step];key=step==0?"2":step==2?"1":"";return;
            case "spring":title="LIGHTER BODIES BOUNCE HIGHER";caption=new[]{"Get small before landing on the spring.","Rise above the ledge, then steer.","Small bounces high. Plan your landing."}[step];key=step==1?"A / D":"2";return;
            case "bridge":title="BALANCE THE OTHER SIDE";caption="Match the crate's weight. Then cross.";
                if(owner is BalanceBridge balance && balance.riderSide>0){caption="Clear the crate. Balance on the right.";key=p.transform.position.x<balance.transform.position.x?"SPACE":"1 / 2";}
                return;
            case "checkpoint":title="A PLACE TO TRY AGAIN";caption=owner is CheckpointBeacon beacon && beacon.Activated?"Saved. Retry from this beacon.":"Touch the blue beacon to save.";key="R";return;
            case "score":title="A QUICKER WAY OUT";caption="Escape sooner for a larger time bonus.";return;
            case "gate":title="MATCH. STAND STILL. PASS.";caption=new[]{"Match the lit body silhouette.","Stay still until the scan turns green.","Access granted. Walk through."}[step];key=step==0 && owner is MassGate gate?(p.Size<gate.requiredSize?"1":"2"):"";return;
            case "capacitor":title="STORE IMPACT. CROSS LIGHT.";caption=new[]{"Get heavy before the landing.","Jump and land on the core.","Get small to conserve the bridge."}[step];key=step==0?"1":step==1?"SPACE":"2";return;
            case "echo":title="LEAVE YOUR WEIGHT BEHIND";caption=new[]{"Become heavy at the projector.","Record your heavy body.","Get small and cross before it fades."}[step];key=new[]{"1","E","2"}[step];return;
            case "shuttle":
                title="YOUR BODY STEERS THE CARRIAGE";caption=new[]{"Small: right. Heavy: left.","Become medium at the cyan dock.","Get small to continue right."}[step];key=step==0?"1 / 2":step==2?"2":p.Size<1?"1":"2";
                if(owner is RailShuttle rail && rail.requireReturn)
                {
                    title="OUTBOUND. REVERSE. DOCK.";
                    if(step==0){caption="Ride to the far stop first.";key="2";}
                    else if(step==1 && Phase==2){caption="Get heavy. Return to the cyan dock.";key="1";}
                    else if(step==1){caption="Become medium to store the return link.";key="2";}
                }
                return;
            case "prism":title="CHANGE SHAPE. ROUTE THE LIGHT.";caption=new[]{"Small lights the upper receiver.","Medium lights the middle receiver.","Heavy lights the lower receiver."}[step];key="1 / 2";return;
            case "relay":title="LINK THIS LANDING";caption=new[]{"Match the lit silhouette.","Hold your shape while the link fills.","Link stored. Find the next route."}[step];key=step==0?"1 / 2":"";return;
            case "airwell":title="ONE WIND SHAFT. THREE ROUTES.";caption=new[]{"Small reaches the high landing.","Medium reaches the middle landing.","Heavy stays near the low landing."}[step];key="1 / 2";return;
            case "freeflight":title="RISE. BRAKE. DESCEND.";caption=new[]{"Small rises freely.","Hold your height. Steer into the side route.","Heavy descends. Brake before the landing."}[step];key=step==1?"A / D":"1 / 2";return;
            case "masslock":title="HOLD NOW. CHANGE LATER.";caption=new[]{"Lock your current shape here.","Movement and food store the next change.","Release when there is room to change."}[step];key=step==0?"E":step==2?"Q":"1 / 2";return;
            default:title="FOLLOW THE FIELD MARKS";caption="Try the action here, then keep going.";return;
        }
    }
    static RectTransform Element(string name,Rect rect,Transform parent)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));var t=go.GetComponent<RectTransform>();t.SetParent(parent,false);
        t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(rect.x,-rect.y);t.sizeDelta=rect.size;return t;
    }
    static Text TextAt(Transform parent,Rect rect,string value,int size,Color color)
    {
        if(!font)font=Resources.Load<Font>("UI/Barlow-Regular")??Font.CreateDynamicFontFromOSFont("Segoe UI",24);
        var t=Element("Chalk lettering",rect,parent).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;
        t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.alignment=TextAnchor.MiddleLeft;return t;
    }
    static Image Picture(Transform parent,string name,Rect rect,Sprite sprite)
    { var image=Element(name,rect,parent).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image; }
    static void Keycap(Transform parent,string key,float x,float y)
    {
        var keys=key.Split('/');
        for(int i=0;i<keys.Length;i++)
        {
            string value=keys[i].Trim();
            float width=value=="SPACE"?113:49;
            float left=x+(keys.Length==1?(113-width)*.5f:i*64);
            var back=Element("Keycap / "+value,new Rect(left,y,width,53),parent).gameObject.AddComponent<KeyboardKeyGraphic>();
            back.color=new Color(.97f,.94f,.82f);back.raycastTarget=false;
            var label=TextAt(parent,new Rect(left+4,y+3,width-8,43),value,value=="SPACE"?22:28,new Color(.05f,.19f,.2f));label.alignment=TextAnchor.MiddleCenter;
        }
    }
    void Arrow(float x,float y,float dx,float dy)
    {
        float endY=Mathf.Clamp(y+dy,Compact?83:104,Compact?145:210);y=Mathf.Clamp(y,Compact?83:104,Compact?145:210);dy=endY-y;
        if(dx==0 && dy==0){Stroke(content,new[]{new Vector2(x-12,-y-5),new Vector2(x+12,-y-5)},Cyan,2);Stroke(content,new[]{new Vector2(x-12,-y+3),new Vector2(x+12,-y+3)},Cyan,2);return;}
        Vector2 a=new Vector2(x,-y),b=new Vector2(x+dx,-y-dy),d=(b-a).normalized,n=new Vector2(-d.y,d.x);
        Stroke(content,new[]{a,(a+b)*.5f+n*1.3f,b},Cyan,2.5f);Stroke(content,new[]{b-d*10+n*6,b,b-d*10-n*6},Cyan,2.5f);
    }
    static void Stroke(Transform parent,Vector2[] points,Color color,float width)
    {var stroke=Element("Chalk stroke",new Rect(0,0,560,260),parent).gameObject.AddComponent<ChalkStroke>();stroke.Points=points;stroke.Width=width;stroke.color=color;stroke.raycastTarget=false;}
}

/// <summary>Square keyboard caps with constant bevels; wide keys change only the straight centre.</summary>
public class KeyboardKeyGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        Panel(vh,r,new Color(.06f,.19f,.21f),4);
        Panel(vh,new Rect(r.x+2,r.y+5,r.width-4,r.height-7),new Color(.61f,.67f,.59f),3);
        Panel(vh,new Rect(r.x+5,r.y+9,r.width-10,r.height-14),color,2);
    }
    static void Panel(VertexHelper vh,Rect r,Color color,float corner)
    {
        int first=vh.currentVertCount;
        var points=new[]{new Vector2(r.xMin+corner,r.yMin),new Vector2(r.xMax-corner,r.yMin),new Vector2(r.xMax,r.yMin+corner),new Vector2(r.xMax,r.yMax-corner),new Vector2(r.xMax-corner,r.yMax),new Vector2(r.xMin+corner,r.yMax),new Vector2(r.xMin,r.yMax-corner),new Vector2(r.xMin,r.yMin+corner)};
        vh.AddVert(r.center,color,Vector2.zero);foreach(var p in points)vh.AddVert(p,color,Vector2.zero);
        for(int i=0;i<8;i++)vh.AddTriangle(first,first+i+1,first+(i+1)%8+1);
    }
}

/// <summary>Code-drawn paint silhouettes keep edges proportional at every display size.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public class ChalkWash : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        var points=new[]{new Vector2(r.xMin+9,r.yMax-2),new Vector2(r.xMax-13,r.yMax),new Vector2(r.xMax,r.yMax-12),new Vector2(r.xMax-3,r.yMin+9),new Vector2(r.xMax-15,r.yMin+1),new Vector2(r.xMin+7,r.yMin),new Vector2(r.xMin,r.yMin+13),new Vector2(r.xMin+2,r.yMax-14)};
        vh.AddVert(r.center,color,Vector2.zero);foreach(var p in points)vh.AddVert(p,color,Vector2.zero);
        for(int i=0;i<points.Length;i++)vh.AddTriangle(0,i+1,(i+1)%points.Length+1);
    }
}
[RequireComponent(typeof(CanvasRenderer))]
public class ChalkStroke : MaskableGraphic
{
    public Vector2[] Points;public float Width=2;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(Points==null)return;
        for(int i=1;i<Points.Length;i++)
        {
            var d=(Points[i]-Points[i-1]).normalized;var n=new Vector2(-d.y,d.x)*Width*.5f;int k=vh.currentVertCount;
            vh.AddVert(Points[i-1]+n,color,Vector2.zero);vh.AddVert(Points[i-1]-n,color,Vector2.zero);vh.AddVert(Points[i]-n,color,Vector2.zero);vh.AddVert(Points[i]+n,color,Vector2.zero);
            vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
    }
}
