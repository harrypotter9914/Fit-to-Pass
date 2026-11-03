using UnityEngine;

/// <summary>Observes mechanism transitions after physics; does not change puzzle behavior.</summary>
[DefaultExecutionOrder(220)]
public class DeviceSound : MonoBehaviour
{
    SoundEmitter sound;
    Vector3 previous;
    float angle, charge;
    float lastMotion;
    int state, size=-1, received;
    bool flag, near, low;
    float previousRock; int rockState; SoundEmitter rockSound;
    MassGate gate; SignalBarrier shutter; MassEcho echo; KineticCapacitor core;
    RailShuttle shuttle; MassPrism prism; BodyRelay relay; VectorAirwell wind;
    PlatformAndRockController counter; PlatformController lift; ElevatorController elevator; BalanceBridge bridge;
    public static void Install()
    {
        foreach(var b in Object.FindObjectsByType<MonoBehaviour>())
            if(b is MassGate || b is SignalBarrier || b is MassEcho || b is KineticCapacitor || b is RailShuttle || b is MassPrism || b is BodyRelay || b is VectorAirwell || b is PlatformAndRockController || b is PlatformController || b is ElevatorController || b is BalanceBridge)
                if(!b.GetComponent<DeviceSound>())b.gameObject.AddComponent<DeviceSound>();
        var p=GameFlow.Instance.Player;if(p && !p.GetComponent<PlayerFoley>())p.gameObject.AddComponent<PlayerFoley>();
        foreach(var b in Object.FindObjectsByType<Rigidbody2D>())
            if(b.bodyType==RigidbodyType2D.Dynamic && !b.GetComponent<PlayerController>() && !b.GetComponent<BalanceBridge>() && !b.GetComponent<ImpactSound>())b.gameObject.AddComponent<ImpactSound>();
    }
    void Start()
    {
        sound=gameObject.AddComponent<SoundEmitter>();previous=transform.position;angle=transform.eulerAngles.z;
        gate=GetComponent<MassGate>();shutter=GetComponent<SignalBarrier>();echo=GetComponent<MassEcho>();core=GetComponent<KineticCapacitor>();
        shuttle=GetComponent<RailShuttle>();prism=GetComponent<MassPrism>();relay=GetComponent<BodyRelay>();wind=GetComponent<VectorAirwell>();
        counter=GetComponent<PlatformAndRockController>();lift=GetComponent<PlatformController>();elevator=GetComponent<ElevatorController>();bridge=GetComponent<BalanceBridge>();
        if(counter) { previous=counter.platformTransform.position; if(counter.rockTransform) { previousRock=counter.rockTransform.position.y; rockSound=counter.rockTransform.gameObject.AddComponent<SoundEmitter>(); } }
        if(lift)previous=lift.platformTransform.position;
    }
    void Motion(string family,float delta)
    {
        int next=Mathf.Abs(delta)>.0003f?(delta>0?1:-1):0;
        if(next!=0)lastMotion=Time.time;
        else if(state!=0 && Time.time-lastMotion<.12f)next=state;
        if(next!=state) { if(next!=0)sound.Cue(family+(next>0?"_up":"_down"));else if(state!=0)sound.Cue(family+"_stop");state=next; }
        sound.Loop(next==0?null:family+"_loop",.24f);
    }
    void FixedUpdate()
    {
        if(GameFlow.InputBlocked)return;
        Vector3 now=counter?counter.platformTransform.position:lift?lift.platformTransform.position:transform.position;
        float dy=now.y-previous.y,dx=now.x-previous.x;
        if(gate)
        {
            if(gate.Progress>0 && charge==0)sound.Cue("scanner_begin");
            if(gate.Progress==0 && charge>0 && !gate.Open)sound.Cue("scanner_cancel");
            if(gate.Open && !flag)sound.Cue("scanner_unlock");
            sound.Loop(Mathf.Abs(dy)>.0003f?"scanner_motor":gate.Progress>0 && !gate.Open?"scanner_scan":null);
            if(state!=0 && Mathf.Abs(dy)<=.0003f)sound.Cue("scanner_stop");
            state=Mathf.Abs(dy)>.0003f?1:0;flag=gate.Open;charge=gate.Progress;
        }
        else if(shutter)Motion("shutter",dy);
        else if(counter)
        {
            Motion("counterweight",dy);
            if(rockSound) {
                float y=counter.rockTransform.position.y, delta=y-previousRock;
                int next=Mathf.Abs(delta)>.0003f?(delta>0?1:-1):0;
                if(next!=rockState)rockSound.Cue(next>0?"stone_up":next<0?"stone_down":"stone_stop");
                rockSound.Loop(next==0?null:"stone_loop",.3f);rockState=next;previousRock=y;
            }
        }
        else if(lift)Motion("pressurelift",dy);
        else if(elevator)Motion("masslift",dy);
        else if(bridge) { float a=transform.eulerAngles.z;Motion("balance",Mathf.Abs(Mathf.DeltaAngle(angle,a))>.08f?Mathf.DeltaAngle(angle,a):0);angle=a; }
        else if(echo)
        {
            if(flag && !echo.Powered)sound.Cue("echo_expire");
            if(echo.Powered && echo.Remaining<4 && !low)sound.Cue("echo_warning");
            low=echo.Powered && echo.Remaining<4;flag=echo.Powered;sound.Loop(flag?"echo_hum":null,.18f);
        }
        else if(core)
        {
            if(flag && !core.Powered)sound.Cue("capacitor_expire");
            if(core.Powered && core.Energy<2 && !low)sound.Cue("capacitor_warning");
            low=core.Powered && core.Energy<2;flag=core.Powered;sound.Loop(flag?"capacitor_hum":null,.2f);
        }
        else if(shuttle)
        {
            if(shuttle.DockProgress<=0 || shuttle.Docked)Motion("shuttle",dx);
            if(shuttle.DockProgress>0 && charge==0)sound.Cue("shuttle_docking");
            if(charge>0 && shuttle.DockProgress==0 && !shuttle.Docked)sound.Cue("shuttle_cancel");
            if(shuttle.DockProgress>0 && !shuttle.Docked)sound.Loop("shuttle_link",.2f);
            charge=shuttle.DockProgress;
        }
        else if(prism)
        {
            if(prism.BodySize!=size && prism.BodySize>=0)sound.Cue("prism_route_"+prism.BodySize);
            if(size>=0 && prism.BodySize<0)sound.Cue("prism_off");
            for(int i=0;i<3;i++)if(prism.Charge[i]>=1 && (received&(1<<i))==0){received|=1<<i;sound.Cue("prism_receiver_"+i);}
            if(prism.Complete && !flag)sound.Cue("prism_complete");
            flag=prism.Complete;size=prism.BodySize;sound.Loop(size>=0?"prism_beam":null,.18f);
        }
        else if(relay)
        {
            var p=GameFlow.Instance.Player;
            bool close=p && Mathf.Abs(p.transform.position.x-transform.position.x)<1.6f && Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-transform.position.y)<.7f && p.Grounded;
            if(close && (!near || p.Size!=size) && p.Size!=relay.requiredSize && !relay.Charged)sound.Cue("relay_reject");
            if(relay.Progress>0 && charge==0)sound.Cue("relay_begin");
            if(relay.Progress==0 && charge>0 && !relay.Charged)sound.Cue("relay_cancel");
            sound.Loop(relay.Progress>0 && !relay.Charged?"relay_charge":null,.2f);charge=relay.Progress;near=close;size=p?p.Size:-1;
        }
        else if(wind)
        {
            string prefix=wind.tiered?"tierfan":"freefan";
            if(wind.Occupied && !flag)sound.Cue(prefix+"_enter");
            if(!wind.Occupied && flag)sound.Cue(prefix+"_exit");
            if(wind.Occupied && size!=wind.BodySize)sound.Cue(prefix+"_"+wind.BodySize);
            sound.Loop(prefix+"_loop",wind.Occupied?.28f:.10f);flag=wind.Occupied;size=wind.BodySize;
        }
        previous=now;
    }
}
