using System;
using System.Linq;
using UnityEngine;

/// <summary>Explicit source/lock relationships used by both feedback and explanation.</summary>
public static class MechanismConnections
{
    public static string Code(Component source)
    {
        if(source is MassEcho)return "E1";
        if(source is MassPrism)return "P1";
        if(source is BodyRelay relay)return "R"+relay.RouteNumber;
        if(source is RailShuttle rail)
        {
            var all=UnityEngine.Object.FindObjectsByType<RailShuttle>().OrderBy(r=>r.left).ToArray();
            return "D"+(Array.IndexOf(all,rail)+1);
        }
        return source is RouteCircuit?"EXIT":"LINK";
    }
    public static string Direction(Vector3 from,Vector3 to)
    {
        var d=to-from;
        if(Mathf.Abs(d.y)>3 && Mathf.Abs(d.y)>Mathf.Abs(d.x)*.65f)return d.y>0?"ABOVE":"BELOW";
        return d.x<0?"LEFT":"RIGHT";
    }
    public static string Relation(Component source,PlayerController player)
    {
        if(!source)return "Match the picture, try the action, then continue along the route.";
        if(source is MassEcho)
        {
            var gate=UnityEngine.Object.FindObjectsByType<SignalBarrier>().FirstOrDefault(g=>g.Source==source);
            return gate?Code(source)+" powers the marked gate to your "+Direction(source.transform.position,gate.transform.position)+". Its echo does not add an exit link.":
                "The echo holds its gate open while you leave the pad and cross.";
        }
        if(source is MassPrism)return "P1 powers its marked locks and is an exit requirement. Finish all three receivers.";
        if(source is BodyRelay relay)return Code(source)+" / "+relay.RouteLabel+" adds one EXIT LINK. Complete all required links to leave.";
        if(source is MassGate scanner)return "This scanner asks for "+new[]{"S / SMALL","M / MEDIUM","L / HEAVY"}[scanner.requiredSize]+". Match it and stand still until green.";
        if(source is RailShuttle)return Code(source)+" is an EXIT LINK. Riding across without a proper dock does not store it.";
        if(source is MassLockStation)return "Keep the shape you need during a crossing. Release the next shape after landing.";
        if(source is SignalBarrier barrier)
        {
            var target=barrier.Source is RouteCircuit c?c.NextSource:barrier.Source;
            return barrier.Requirement+" "+(target && player?Code(target)+" is "+Direction(player.transform.position,target.transform.position)+".":"");
        }
        if(source is ButtonController plate && plate.PowerSource)
            return "This lift also needs "+Code(plate.PowerSource)+". Light all optical receivers before pressing Heavy.";
        if(source is LevelGoal)
        {
            var circuit=UnityEngine.Object.FindAnyObjectByType<RouteCircuit>();
            return circuit?circuit.MissingReason:"This door finishes the chapter and unlocks the next one.";
        }
        if(source is KineticCapacitor)return "Heavy landing powers the temporary bridge. A small body spends less energy crossing.";
        if(source is VectorAirwell wind)return wind.tiered?"Enter the wind and choose a route height with your body. A / D leaves sideways.":
            "Medium holds height only inside the wind. Leave sideways when lined up with an opening.";
        return "This device changes your route. Follow the shown action, then reach the next landing.";
    }
    public static void Connect(Component source,Collider2D target,Func<bool> powered)
    {
        if(!(source is MassEcho) && !(source is MassPrism))return;
        var go=new GameObject("Visible power link "+Code(source));
        var link=go.AddComponent<PowerConnectionVisual>();link.Initialize(source,target,powered);
    }
}

public class PowerConnectionVisual : MonoBehaviour
{
    public Component Source { get; private set; }
    LineRenderer wire;
    TextMesh a,b;
    Func<bool> powered;
    public void Initialize(Component source,Collider2D target,Func<bool> state)
    {
        Source=source;powered=state;
        float floor=target.bounds.min.y;
        var from=new Vector3(source.transform.position.x,source.transform.position.y+.045f);
        var to=new Vector3(target.bounds.center.x,floor+.045f);
        wire=gameObject.AddComponent<LineRenderer>();wire.sharedMaterial=Art.Material;wire.sortingOrder=2;
        wire.useWorldSpace=true;wire.positionCount=3;wire.startWidth=wire.endWidth=.045f;
        wire.SetPositions(new[]{from,new Vector3(to.x,from.y),to});
        a=Badge(MechanismConnections.Code(source),from+new Vector3(-1.8f,1));
        b=Badge(MechanismConnections.Code(source),to+new Vector3(1.8f,1));
    }
    TextMesh Badge(string text,Vector3 position)
    {
        var go=new GameObject("Matching source code "+text);go.transform.SetParent(transform);go.transform.position=position;
        var t=go.AddComponent<TextMesh>();t.text=text;t.fontSize=42;t.characterSize=.04f;t.anchor=TextAnchor.MiddleCenter;
        go.GetComponent<MeshRenderer>().sortingOrder=12;return t;
    }
    void Update()
    {
        if(!wire)return;
        var color=powered!=null && powered()?new Color(.22f,.95f,.9f):new Color(.95f,.64f,.27f);
        wire.startColor=wire.endColor=color;a.color=b.color=color;
    }
}
