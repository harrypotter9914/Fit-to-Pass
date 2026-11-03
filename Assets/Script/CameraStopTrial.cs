#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Actual movement/release trials and render-frame traces; excluded from builds.</summary>
[DefaultExecutionOrder(10000)]
public class CameraStopTrial : MonoBehaviour
{
    public static CameraStopTrial Instance;
    public bool Finished;
    [Serializable] public class Frame
    {
        public string phase;
        public float time, age, dt, actorX, actorY, cameraX, cameraY;
    }
    [Serializable] class Trace { public List<Frame> frames; }
    readonly List<Frame> frames = new List<Frame>();
    readonly string[] phases = { "settle", "right", "stop_right", "left", "stop_left", "tap_right", "stop_tap_right", "tap_left", "stop_tap_left", "jump", "land", "right_again", "reverse", "stop_reverse" };
    readonly float[] axes = { 0, 1, 0, -1, 0, 1, 0, -1, 0, 0, 0, 1, -1, 0 };
    float[] durations;
    float began;
    int phase;
    string output;
    PlayerController player;
    public static void Begin(int weight, int fps, string path)
    {
        if (Instance) Destroy(Instance.gameObject);
        var p = GameFlow.Instance.Player;
        p.Restore(weight,5,5);
        p.transform.position=RespawnSafety.Root(p,new Vector2(4,0),p.Size);
        p.Body.position=p.transform.position; p.Body.linearVelocity=Vector2.zero;
        Physics2D.SyncTransforms(); p.GetComponent<CameraFollow>().Snap();
        p.TestInputActive=true; p.TestHorizontal=0; p.TestJumpHeld=false;
        TutorialDirector.TestBypass=true; Application.targetFrameRate=fps; Time.timeScale=1;
        var trial=new GameObject("Camera stop/reverse trial").AddComponent<CameraStopTrial>();
        Instance=trial; trial.player=p;trial.output=path;trial.began=Time.unscaledTime;
        float travel=6f/(p.Size==0?10f:p.Size==2?2.5f:5f);
        trial.durations=new[]{.4f,travel,1.4f,travel,1.4f,.1f,.8f,.1f,.8f,.75f,.8f,.4f,.4f,1.4f};
    }
    void Update()
    {
        if(Finished || !player)return;
        if(Time.unscaledTime-began>=durations[phase])
        {
            phase++;began=Time.unscaledTime;
            if(phase==phases.Length)
            {
                player.TestHorizontal=0;player.TestJumpHeld=false;Finished=true;
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output,JsonUtility.ToJson(new Trace{frames=frames}));return;
            }
            if(phases[phase]=="jump")player.TestJumpPressed=true;
        }
        player.TestHorizontal=axes[phase];player.TestJumpHeld=phases[phase]=="jump";
    }
    void LateUpdate()
    {
        if(Finished || !player || !Camera.main)return;
        var camera=Camera.main.transform.position;
        frames.Add(new Frame{phase=phases[phase],time=Time.unscaledTime,age=Time.unscaledTime-began,dt=Time.unscaledDeltaTime,
            actorX=player.transform.position.x,actorY=player.transform.position.y,cameraX=camera.x,cameraY=camera.y});
    }
}
#endif
