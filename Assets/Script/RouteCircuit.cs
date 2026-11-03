using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Visible, named objectives make branches meaningful and prevent exit shortcuts.</summary>
public class RouteCircuit : MonoBehaviour
{
    public readonly List<BodyRelay> Links = new List<BodyRelay>();
    public readonly List<string> Labels = new List<string>();
    public readonly List<System.Func<bool>> Interlocks = new List<System.Func<bool>>();
    public readonly List<string> InterlockLabels = new List<string>();
    public readonly List<Component> InterlockSources = new List<Component>();
    public int Total => Links.Count+Interlocks.Count;
    public int Done => Links.Count(x=>x && x.Charged)+Interlocks.Count(x=>x());
    public string MissingReason
    {
        get
        {
            for(int i=0;i<Links.Count;i++)if(!Links[i].Charged)return "EXIT LOCKED: LIGHT R"+(i+1)+" / "+Labels[i]+".";
            for(int i=0;i<Interlocks.Count;i++)if(!Interlocks[i]())return "EXIT LOCKED: "+InterlockLabels[i]+".";
            return "ALL EXIT LINKS READY. ENTER THE EXIT.";
        }
    }
    public Component NextSource
    {
        get
        {
            for(int i=0;i<Links.Count;i++)if(!Links[i].Charged)return Links[i];
            for(int i=0;i<Interlocks.Count;i++)if(!Interlocks[i]())return i<InterlockSources.Count?InterlockSources[i]:null;
            return null;
        }
    }
    public bool Complete => Links.All(x => x && x.Charged) && Interlocks.All(x => x());
    public string NextObjective
    {
        get
        {
            int total = Links.Count + Interlocks.Count;
            int done = Links.Count(x => x && x.Charged) + Interlocks.Count(x => x());
            for (int i = 0; i < Links.Count; i++) if (!Links[i].Charged) return "EXIT LINKS "+done + " / " + total + "  ·  NEXT R"+(i+1)+" / "+Labels[i];
            for (int i = 0; i < Interlocks.Count; i++) if (!Interlocks[i]()) return "EXIT LINKS "+done + " / " + total + "  ·  "+InterlockLabels[i];
            return "EXIT READY  ·  ALL LINKS RESTORED";
        }
    }
}
