using UnityEngine;

/// <summary>A linked shutter. Never closes through the player when power expires.</summary>
public class SignalBarrier : MonoBehaviour
{
    public System.Func<bool> Powered;
    public Component Source;
    public string Requirement
    {
        get
        {
            if(Source is MassEcho echo)return echo.Powered?"ECHO POWER ACTIVE. GATE OPEN.":"RECORD HEAVY + E AT "+MechanismConnections.Code(echo)+".";
            if(Source is MassPrism prism)
            {
                string missing="";
                for(int i=0;i<3;i++)if(prism.Charge[i]<1)missing+=(missing.Length>0?" / ":"")+new[]{"TOP","MIDDLE","LOW"}[i];
                return prism.Complete?"ALL RECEIVERS LIT. GATE OPEN.":"LIGHT MISSING RECEIVERS: "+missing+".";
            }
            if(Source is RouteCircuit circuit)return circuit.MissingReason;
            return "RESTORE THIS GATE'S MARKED POWER SOURCE.";
        }
    }
    Vector3 closed;
    Collider2D box;
    RecessedPanel recess;
    public bool Open { get; private set; }
    void Start()
    {
        closed = transform.position; box = GetComponent<Collider2D>();
        recess = gameObject.AddComponent<RecessedPanel>(); recess.Initialize(closed);
        MechanismConnections.Connect(Source,box,()=>Powered!=null && Powered());
        GamePresentation.WorldLabel("GATE / "+MechanismConnections.Code(Source),closed+Vector3.up*3.1f,new Color(.95f,.75f,.38f));
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked) return;
        var p = GameFlow.Instance.Player;
        Open = Powered != null && Powered();
        if (!Open && p && transform.position.y > closed.y + .5f)
        {
            Bounds sweep = recess.Enclosure;
            sweep.Expand(new Vector3(.3f, .1f, 1));
            Open = sweep.Intersects(p.GetComponent<Collider2D>().bounds);
        }
        transform.position = Vector3.MoveTowards(transform.position, closed + Vector3.up * (Open ? 5.5f : 0), Time.fixedDeltaTime * 4);
        recess.Refresh();
    }
}
