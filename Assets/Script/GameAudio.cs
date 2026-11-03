using System.Collections.Generic;
using UnityEngine;

/// <summary>Cached SFX, bounded voices, distance-aware emitters and crossfaded regional music.</summary>
public class GameAudio : MonoBehaviour
{
    static GameAudio instance;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly HashSet<string> missing = new HashSet<string>();
    AudioSource[] voices, music;
    bool[] worldVoice;
    float[] voiceGain;
    int nextVoice, activeMusic;
    float fade;
    bool suspended;
    public string CurrentTrack { get; private set; }
    public static float MusicVolume { get => PlayerPrefs.GetFloat("Fit.MusicVolume", .55f); set => PlayerPrefs.SetFloat("Fit.MusicVolume", Mathf.Clamp01(value)); }
    public static float EffectsVolume { get => PlayerPrefs.GetFloat("Fit.EffectsVolume", .8f); set => PlayerPrefs.SetFloat("Fit.EffectsVolume", Mathf.Clamp01(value)); }
#if UNITY_EDITOR
    public static readonly Dictionary<string, int> Played = new Dictionary<string, int>();
#endif
    void Awake()
    {
        instance = this;
        voices = new AudioSource[16]; worldVoice = new bool[16]; voiceGain = new float[16];
        for (int i=0;i<voices.Length;i++) { voices[i]=gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake=false; }
        music = new AudioSource[2];
        for (int i=0;i<2;i++) { music[i]=gameObject.AddComponent<AudioSource>(); music[i].loop=true; music[i].playOnAwake=false; music[i].volume=0; }
#if UNITY_EDITOR
        Played.Clear();
#endif
    }
    void OnDestroy() { if(instance==this)instance=null; }
    public static AudioClip Clip(string name)
    {
        if (!instance || string.IsNullOrEmpty(name)) return null;
        if (!instance.clips.TryGetValue(name, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/Designed/"+name) ?? Resources.Load<AudioClip>("Audio/"+name);
            instance.clips[name]=clip;
            if(!clip && instance.missing.Add(name))Debug.LogError("Missing designed audio: "+name);
        }
        return clip;
    }
    public static void Mark(string name)
    {
#if UNITY_EDITOR
        Played[name] = Played.TryGetValue(name,out var n) ? n+1 : 1;
#endif
    }
    public static float Attenuation(Vector3 position)
    {
        var p=GameFlow.Instance ? GameFlow.Instance.Player : null;
        if(!p)return 0;
        float d=Vector2.Distance(p.transform.position,position);
        return Mathf.Pow(Mathf.Clamp01(1-d/22),2);
    }
    public static float Pan(Vector3 position)
    {
        var p=GameFlow.Instance ? GameFlow.Instance.Player : null;
        return p ? Mathf.Clamp((position.x-p.transform.position.x)/14,-.7f,.7f) : 0;
    }
    public static void Play(string name, float volume=1) => Emit(name,volume,false,0);
    public static void At(string name, Vector3 position, float volume=.65f)
    {
        if(GameFlow.InputBlocked)return;
        Emit(name, volume*Attenuation(position),true,Pan(position));
    }
    static void Emit(string name,float gain,bool world,float pan)
    {
        if(!instance || gain<.005f)return;
        var clip=Clip(name); if(!clip)return;
        int index=instance.nextVoice++ % instance.voices.Length;
        var source=instance.voices[index]; source.Stop(); source.clip=clip;
        instance.worldVoice[index]=world; instance.voiceGain[index]=gain;
        source.panStereo=pan; source.volume=gain*EffectsVolume; source.Play(); Mark(name);
    }
    public static void ScreenChanged(FlowScreen before, FlowScreen after)
    {
        if(before==after)return;
        if(after==FlowScreen.Paused)Play("ui_pause",.45f);
        else if(before==FlowScreen.Paused && after==FlowScreen.Playing)Play("ui_resume",.4f);
        else if(after==FlowScreen.Tutorial || after==FlowScreen.Discovery)Play("guide_open",.38f);
        else if(after==FlowScreen.NewGameConfirm)Play("ui_confirm",.4f);
    }
    string DesiredTrack(GameFlow f)
    {
        if(f.Screen==FlowScreen.Opening)return "music_opening";
        if(f.Screen==FlowScreen.Ending || f.Screen==FlowScreen.Credits)return "music_ending";
        if(f.Screen==FlowScreen.Dead)return "music_failure";
        if(f.Screen==FlowScreen.Complete)return "music_victory";
        if(f.Stage>0 && f.Screen!=FlowScreen.Menu && f.Screen!=FlowScreen.Stages && f.Screen!=FlowScreen.NewGameConfirm)
            return "music_chapter_" + CampaignMap.Music(f.Stage);
        return "music_menu";
    }
    void Update()
    {
        var f=GameFlow.Instance;if(!f)return;
        string wanted=DesiredTrack(f);
        if(wanted!=CurrentTrack)
        {
            var clip=Clip(wanted);
            if(clip) { activeMusic=1-activeMusic; music[activeMusic].Stop();music[activeMusic].clip=clip;music[activeMusic].volume=0;music[activeMusic].Play();CurrentTrack=wanted;fade=0;Mark(wanted); }
        }
        fade=Mathf.Min(1,fade+Time.unscaledDeltaTime/1.5f);
        float duck=f.Screen==FlowScreen.Paused || f.Screen==FlowScreen.Settings || f.Screen==FlowScreen.Tutorial ? .42f : 1;
        for(int i=0;i<2;i++)
        {
            music[i].volume=MusicVolume*.5f*duck*(i==activeMusic?fade:1-fade);
            if(i!=activeMusic && fade>=1 && music[i].isPlaying)music[i].Stop();
        }
        bool pause=GameFlow.InputBlocked || f.Loading;
        if(pause!=suspended)
        {
            for(int i=0;i<voices.Length;i++)if(worldVoice[i]) { if(pause)voices[i].Pause();else voices[i].UnPause(); }
            suspended=pause;
        }
        for(int i=0;i<voices.Length;i++) voices[i].volume=voiceGain[i]*EffectsVolume;
    }
}
