#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Observes actual first-use cards during input-driven route tests.</summary>
[DefaultExecutionOrder(10000)]
public class FirstEncounterPlaytestRecorder : MonoBehaviour
{
    public static FirstEncounterPlaytestRecorder Instance;
    public string Folder;
    public int Samples, PlayerOverlap, DeviceOverlap;
    float next;
    readonly HashSet<string> captured=new HashSet<string>();
    [Serializable] class Row
    {
        public int chapter;
        public string topic,owner,image;
        public float readSeconds;
        public Rect panel;
        public bool playerOverlap,deviceOverlap;
    }
    public static void Begin(string folder)
    {
        var p=GameFlow.Instance.Player;
        Instance=p.gameObject.AddComponent<FirstEncounterPlaytestRecorder>();
        Instance.Folder=folder;Directory.CreateDirectory(folder);
    }
    void LateUpdate()
    {
        if(Time.unscaledTime<next)return;
        next=Time.unscaledTime+.12f;
        var f=GameFlow.Instance;var g=FirstEncounterGuide.Instance;
        if(!f || f.Loading || f.Screen!=FlowScreen.Playing || !f.Player || !g || !g.LocalVisible || g.Local==null)return;
        var rect=g.Panel;
        bool po=rect.Overlaps(FirstEncounterGuide.Project(f.Player.GetComponent<Collider2D>().bounds));
        bool device=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Any(r=>r.enabled && r.gameObject.activeInHierarchy && r.sortingOrder>=4 && !r.GetComponentInParent<PlayerController>() && rect.Overlaps(FirstEncounterGuide.Project(r.bounds)));
        Samples++;PlayerOverlap+=po?1:0;DeviceOverlap+=device?1:0;
        var row=new Row{chapter=f.Stage,topic=g.Local.Lesson.Id,owner=g.Local.OwnerName,readSeconds=g.ReadSeconds,panel=rect,playerOverlap=po,deviceOverlap=device};
        if(g.ReadSeconds>1 && captured.Add(row.topic))
        {
            row.image="chapter-"+f.Stage.ToString("00")+"-"+row.topic+".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder,row.image));
        }
        File.AppendAllText(Path.Combine(Folder,"observations.jsonl"),JsonUtility.ToJson(row)+"\n");
    }
}
#endif
