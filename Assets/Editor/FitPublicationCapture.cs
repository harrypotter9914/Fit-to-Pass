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
public static class FitPublicationCapture
{
    const string Flag="Fit.PublicationCapture.Active", Backup="Fit.PublicationCapture.Prefs";
    static string Root=>Path.GetFullPath("output/itch-2.4");
    static string Media=>Path.Combine(Root,"media");
    static EditorWindow view;
    static double started;
    static bool running,finished;
    static readonly List<string> notes=new List<string>();
    [Serializable] class Preference { public string key;public int value; }
    [Serializable] class Preferences { public List<Preference> values=new List<Preference>(); }
    static FitPublicationCapture(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.quitting+=Restore;}
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
        Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;Time.captureFramerate=15;
        TutorialDirector.TestBypass=true;yield return new WaitForSecondsRealtime(.5f);
        yield return Still("00-title-screen");
        yield return Ready(2);Place(30,0,3);yield return new WaitForSeconds(.4f);
        yield return Still("01-greenhouse-passage");
        Place(7,0,3);yield return new WaitForSeconds(.2f);
        yield return Clip("size-shifting",7,i=>Aim(i<25?0:i<65?1:2,i),82,"size-heavy");

        yield return Ready(8);var lift=Object.FindObjectsByType<ElevatorController>().OrderBy(e=>Mathf.Abs(e.transform.position.x-15)).First();
        OnPad(lift,2);yield return new WaitForSeconds(.3f);
        yield return Clip("mass-lift",8,i=>Aim(i<35?0:i<75?1:2,i),33,"02-mass-lift");

        yield return Ready(4);var echo=Object.FindAnyObjectByType<MassEcho>();OnPad(echo,14);yield return new WaitForSeconds(.4f);
        yield return Clip("heavy-echo",9,i=>{if(i==10)Note("Echo recorded="+echo.Record());if(i>=24)Aim(0,i);if(i>=47)GameFlow.Instance.Player.TestHorizontal=-1;},38,"03-heavy-echo");

        yield return Ready(9);var prism=Object.FindAnyObjectByType<MassPrism>();OnPad(prism,2);yield return new WaitForSeconds(.4f);
        yield return Clip("optical-circuit",8,i=>Aim(i<32?0:i<65?1:2,i),108,"04-optical-foundry");
        Note("Prism receivers="+string.Join(",",prism.Charge.Select(v=>v.ToString("F2"))));

        yield return Ready(8);Place(128,2,3);yield return new WaitForSeconds(.4f);yield return Still("05-foundry-crossing");
        yield return Ready(13);var bridge=Object.FindObjectsByType<BalanceBridge>().OrderBy(b=>b.transform.position.x).First();
        Place(bridge.transform.position.x+bridge.riderSide*3.5f,bridge.transform.position.y+.5f,8);yield return new WaitForSeconds(.7f);yield return Still("06-cold-balance");

        yield return Ready(14);var wind=Object.FindObjectsByType<VectorAirwell>().First(w=>!w.tiered);
        Place(wind.transform.position.x,wind.transform.position.y+1,3);
        var follow=GameFlow.Instance.Player.GetComponent<CameraFollow>();follow.enabled=false;
        Camera.main.orthographicSize=11.5f;Camera.main.transform.position=new Vector3(wind.transform.position.x,wind.transform.position.y+10,-10);
        yield return new WaitForSeconds(.3f);
        yield return Clip("free-flight-wind",8,i=>Aim(i<35?0:i<75?1:2,i),57,"07-free-flight");

        yield return Ready(4);echo=Object.FindAnyObjectByType<MassEcho>();OnPad(echo,14);yield return new WaitForSeconds(.4f);
        var guide=FirstEncounterGuide.Instance;guide.ClearPending();PlayerPrefs.DeleteKey(FirstEncounterGuide.Key("echo"));guide.Request("echo",echo);TutorialDirector.TestBypass=false;
        yield return new WaitForSeconds(.6f);yield return Still("08-first-encounter-guide");
        GameFlow.Instance.SetScreen(FlowScreen.Paused);yield return Still("09-pause-menu");
        Note("COMPLETE: production gameplay captured; publication setup uses isolated editor preferences.");Finish(0);
    }
}
