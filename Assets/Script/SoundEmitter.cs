using UnityEngine;

/// <summary>One continuous voice per physical source, with local distance and pause behavior.</summary>
public class SoundEmitter : MonoBehaviour
{
    AudioSource source;
    string loopName;
    float gain;
    bool paused;
    public string LoopName => loopName;
    public AudioSource Source => source;
    void Awake() { source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.volume=0; }
    public void Cue(string name,float volume=.65f) => GameAudio.At(name,transform.position,volume);
    public void Loop(string name,float volume=.25f)
    {
        gain=volume;
        if(name==loopName)return;
        source.Stop();loopName=name;paused=false;
        if(string.IsNullOrEmpty(name))return;
        source.clip=GameAudio.Clip(name);
        if(source.clip){source.Play();GameAudio.Mark(name);}
    }
    void Update()
    {
        bool block=GameFlow.InputBlocked || !GameFlow.Instance || GameFlow.Instance.Loading;
        if(block!=paused) { if(block)source.Pause();else source.UnPause();paused=block; }
        source.volume=Mathf.MoveTowards(source.volume,gain*GameAudio.EffectsVolume*GameAudio.Attenuation(transform.position),Time.unscaledDeltaTime*2);
        source.panStereo=GameAudio.Pan(transform.position);
    }
    void OnDisable() { if(source)source.Stop();loopName=null; }
}
