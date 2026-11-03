using UnityEngine;

/// <summary>A visible power connection avoids putting a tall shutter through an overhead rail.</summary>
public class OpticalLiftPower : MonoBehaviour
{
    MassPrism prism;
    LineRenderer cable, lamp;
    TextMesh label;
    public static void Connect(MassPrism source, ButtonController target, float floor)
    {
        var go=new GameObject("Optical power / freight lift");
        var power=go.AddComponent<OpticalLiftPower>();power.prism=source;
        target.PowerReady=()=>source && source.Complete;
        target.PowerSource=source;
        GamePresentation.WorldLabel("P1",source.transform.position+new Vector3(-1.8f,1),new Color(.95f,.75f,.38f));
        target.PowerHint="Light all three optical receivers to power this lift.";
        power.cable=power.Line("Recessed power cable",new[]{new Vector3(source.transform.position.x,floor+.025f),new Vector3(target.transform.position.x,floor+.025f)},.05f,2);
        var at=new Vector3(target.transform.position.x-1.05f,floor+.55f);
        var stem=power.Line("Indicator stem",new[]{new Vector3(at.x,floor+.025f),at},.04f,2);stem.startColor=stem.endColor=new Color(.25f,.43f,.4f);
        power.lamp=power.Line("Power indicator",new[]{at+new Vector3(-.13f,0),at+new Vector3(0,.13f),at+new Vector3(.13f,0),at+new Vector3(0,-.13f),at+new Vector3(-.13f,0)},.06f,9);
        var text=new GameObject("Optical power status");text.transform.SetParent(go.transform);text.transform.position=new Vector3(target.transform.position.x-3.3f,floor+2.15f);
        power.label=text.AddComponent<TextMesh>();power.label.anchor=TextAnchor.MiddleCenter;power.label.fontSize=36;power.label.characterSize=.035f;text.GetComponent<MeshRenderer>().sortingOrder=12;
    }
    LineRenderer Line(string name,Vector3[] points,float width,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(transform);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Art.Material;
        line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.sortingOrder=order;return line;
    }
    void Update()
    {
        bool ready=prism && prism.Complete;var color=ready?new Color(.25f,.95f,.85f):new Color(.92f,.53f,.16f);
        cable.startColor=cable.endColor=Color.Lerp(new Color(.12f,.2f,.2f),color,ready?.8f:.45f);
        lamp.startColor=lamp.endColor=color;label.text=ready?"P1 / LIFT READY":"P1 / LIGHT ALL RECEIVERS";label.color=color;
    }
}
