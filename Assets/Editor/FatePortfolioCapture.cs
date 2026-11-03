using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Editor-only media setup; records the production game, never changes the player build.</summary>
[InitializeOnLoad]
public static class FatePortfolioCapture
{
    const string Flag="Fate.PortfolioCapture.Active", Backup="Fate.PortfolioCapture.Prefs";
    static string Root=>Path.GetFullPath("output/portfolio/capture");
    static string Media=>Path.Combine(Root,"media");
    static EditorWindow view;
    static double started;
    static bool running,finished;
    static readonly List<string> notes=new List<string>();
    [Serializable] class Preference { public string key;public int value; }
    [Serializable] class Preferences { public List<Preference> values=new List<Preference>(); }
    static FatePortfolioCapture(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.quitting+=Restore;}
    public static void Begin()
    {
        Directory.CreateDirectory(Media);
        var saved=new Preferences();
        var topics=new[]{"movement","jump","food","mass","metabolism","heat","cold","ambient","score","duct"}.Concat(FirstEncounterGuide.Lessons.Keys).Distinct();
        var keys=new[]{"Fit.Unlocked","Fit.ResumeStage","Fit.HasRun","Fit.StorySeen","Fit.CampaignVersion"}
            .Concat(Enumerable.Range(1,15).SelectMany(i=>new[]{"Fit.Best."+i,"Fit.Completed."+i}))
            .Concat(topics.SelectMany(t=>new[]{"Fit.Guide.v4."+t,"Fit.Discovered.v4."+t,"Fit.Explained.v5."+t}));
        foreach(var k in keys)saved.values.Add(new Preference{key=k,value=PlayerPrefs.GetInt(k,-1)});
        string json=JsonUtility.ToJson(saved,true);SessionState.SetString(Backup,json);File.WriteAllText(Path.Combine(Root,"editor-prefs-before.json"),json);
        PlayerPrefs.SetInt("Fit.Unlocked",15);
        foreach(var t in topics){PlayerPrefs.SetInt("Fit.Explained.v5."+t,1);PlayerPrefs.SetInt("Fit.Guide.v4."+t,1);}
        PlayerPrefs.Save();SessionState.SetBool(Flag,true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        ShowGame();EditorApplication.EnterPlaymode();
    }
    static void ShowGame()
    {
        try
        {
            var asm=typeof(Editor).Assembly;var t=asm.GetType("UnityEditor.GameView");
            view=EditorWindow.GetWindow(t);view.Show();view.Focus();
            var sizes=asm.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizes);
            var instance=singleton.GetProperty("instance").GetValue(null);
            var group=sizes.GetMethod("GetGroup").Invoke(instance,new object[]{0});
            var sizeType=asm.GetType("UnityEditor.GameViewSize");var enumType=asm.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.ToObject(enumType,1),1600,900,"Fit publication 1600x900"},null);
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            t.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);
        }
        catch(Exception e){Debug.Log("Publication capture uses current Game View size: "+e.Message);}
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag,false))return;
        started=EditorApplication.timeSinceStartup;EditorApplication.update+=Pump;
    }
    static void Pump()
    {
        if(finished)return;
        EditorApplication.QueuePlayerLoopUpdate();if(view)view.Repaint();
        if(EditorApplication.timeSinceStartup-started>420){Note("TIMEOUT");Finish(2);return;}
        if(!running && GameFlow.Instance){running=true;GameFlow.Instance.StartCoroutine(Run());}
    }
    static void Note(string s){notes.Add(s);File.WriteAllLines(Path.Combine(Root,"capture-log.txt"),notes);Debug.Log("FIT MEDIA "+s);}
    static void Restore()
    {
        if(!SessionState.GetBool(Flag,false))return;
        var saved=JsonUtility.FromJson<Preferences>(SessionState.GetString(Backup,"{}"));
        foreach(var p in saved.values){if(p.value==-1)PlayerPrefs.DeleteKey(p.key);else PlayerPrefs.SetInt(p.key,p.value);}
        PlayerPrefs.Save();SessionState.SetBool(Flag,false);
        File.WriteAllText(Path.Combine(Root,"editor-progress-restored.txt"),"Restored "+saved.values.Count+" original editor preference keys. Native player saves were not accessed.\n");
    }
    static void Finish(int result){if(finished)return;finished=true;Time.captureFramerate=0;Time.timeScale=1;Restore();EditorApplication.Exit(result);}
    static IEnumerator Ready(int stage)
    {
        TutorialDirector.TestBypass=true;FirstEncounterGuide.Instance.ClearPending();GameFlow.Instance.StartStage(stage);
        while(GameFlow.Instance.Loading || !GameFlow.Instance.Player || GameFlow.Instance.Stage!=stage)yield return null;
        GameFlow.Instance.Resume();yield return new WaitForSeconds(.3f);
        GameFlow.Instance.Player.TestInputActive=true;GameFlow.Instance.Player.TestHorizontal=0;
    }
    static void Place(float x,float floor,int weight)
    {
        var p=GameFlow.Instance.Player;p.Restore(weight,8,8);
        p.transform.position=RespawnSafety.Root(p,new Vector2(x,floor),p.Size);p.Body.position=p.transform.position;p.Body.linearVelocity=Vector2.zero;
        p.TestHorizontal=0;p.TestJumpHeld=false;Physics2D.SyncTransforms();p.GetComponent<CameraFollow>().Snap();
    }
    static void OnPad(Component device,int weight)
    {
        var pad=device.GetComponent<Collider2D>();Place(device.transform.position.x,pad?pad.bounds.max.y:device.transform.position.y,weight);
    }
    static void Aim(int size,int frame)
    {
        var p=GameFlow.Instance.Player;if(frame%3==0 && p.Size!=size)p.Consume(p.Size<size);
    }
    static void Save(string file)
    {
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        if(!texture || texture.width<400)throw new Exception("Game View did not produce a usable frame");
        File.WriteAllBytes(file,file.EndsWith(".png")?texture.EncodeToPNG():texture.EncodeToJPG(91));
        Object.Destroy(texture);
    }
    static IEnumerator Still(string name)
    {
        yield return new WaitForEndOfFrame();Save(Path.Combine(Media,name+".png"));Note("STILL "+name+" "+Screen.width+"x"+Screen.height);
    }
    static IEnumerator Clip(string name,float seconds,Action<int> action,int stillFrame,string stillName)
    {
        string folder=Path.Combine(Root,"frames",name);Directory.CreateDirectory(folder);int count=Mathf.RoundToInt(seconds*15);
        Note("START "+name+" stage="+GameFlow.Instance.Stage);
        for(int i=0;i<count;i++)
        {
            if(GameFlow.Instance.Screen==FlowScreen.Paused)GameFlow.Instance.Resume();
            action?.Invoke(i);yield return new WaitForEndOfFrame();
            Save(Path.Combine(folder,i.ToString("D4")+".jpg"));
            if(i==stillFrame)Save(Path.Combine(Media,stillName+".png"));
        }
        Note("DONE "+name+" frames="+count+" position="+GameFlow.Instance.Player.transform.position);
    }
    static IEnumerator Run()
    {
        Application.runInBackground=true;QualitySettings.vSyncCount=0;Time.captureFramerate=30;
        TutorialDirector.TestBypass=true;yield return Ready(6);
        var c=Object.FindAnyObjectByType<PlatformAndRockController>();
        var plate=c.platformTransform.GetComponent<Collider2D>();
        Place(c.platformTransform.position.x,plate.bounds.max.y,14);
        yield return new WaitForSeconds(7);
        var player=GameFlow.Instance.Player;
        var follow=player.GetComponent<CameraFollow>();follow.enabled=false;
        float plateX=c.platformTransform.position.x,rockX=c.rockTransform.position.x;
        float ground=c.rockDownPosition.y-1.5f;
        Camera.main.orthographicSize=6.0f;
        Camera.main.transform.position=new Vector3((plateX+rockX)*.5f+2,ground+4.4f,-10);
        yield return new WaitForSeconds(.15f);
        yield return Still("counter-01-open");
        Note("COUNTER plate="+plateX+" rock="+rockX+" root="+player.transform.position+" ground="+ground);
        player.TestHorizontal=1;
        int frame=0;bool middle=false,crossed=false;
        while(frame<450)
        {
            if(frame>20 && frame%3==0 && player.Size!=0)player.Consume(false);
            if(!middle && player.Size==0 && player.transform.position.x>plateX+3)
            {
                yield return Still("counter-02-shrink-run");middle=true;
                Note("SHRINK root="+player.transform.position+" stone="+c.rockTransform.position);
            }
            if(player.transform.position.x>rockX+3)
            {
                player.TestHorizontal=0;yield return new WaitForSeconds(.15f);
                yield return Still("counter-03-clear");crossed=true;break;
            }
            frame++;yield return null;
        }
        Note("CROSSED="+crossed);
        yield return new WaitForSeconds(7);
        Place(plateX+2.5f,ground,3);yield return new WaitForSeconds(.3f);
        player.TestHorizontal=1;yield return new WaitForSeconds(1.7f);player.TestHorizontal=0;
        yield return Still("counter-04-blocked");
        yield return Ready(14);
        var wind=Object.FindObjectsByType<VectorAirwell>().First(w=>!w.tiered);
        Place(wind.transform.position.x,wind.transform.position.y+1,3);
        player=GameFlow.Instance.Player;follow=player.GetComponent<CameraFollow>();follow.enabled=false;
        Camera.main.orthographicSize=7.2f;
        Camera.main.transform.position=new Vector3(wind.transform.position.x+2,wind.transform.position.y+7.5f,-10);
        yield return new WaitForSeconds(.8f);yield return Still("wind-01-rise");
        while(player.GetComponent<Collider2D>().bounds.min.y<9.15f)yield return null;
        while(player.Size==0){player.Consume(true);yield return new WaitForSeconds(.07f);}
        yield return new WaitForSeconds(.3f);yield return Still("wind-02-brake");
        player.TestHorizontal=1;yield return new WaitForSeconds(1.2f);player.TestHorizontal=0;
        yield return Still("wind-03-side-route");
        Note("COMPLETE: staged production mechanics; screenshots are current-build editor captures, not external usability data.");Finish(0);
    }
}
