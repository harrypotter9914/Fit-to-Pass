using UnityEngine;

/// <summary>Unscaled illustrated prologue followed by a sprite-animated escape and fade.</summary>
public class OpeningSequence : MonoBehaviour
{
    int soundPage=-1;
    float stepAt;
    public float Elapsed
    {
        get; private set;
    }
    public bool Replay
    {
        get; private set;
    }
    public int Page => Mathf.Min(3, (int)(Elapsed / 5));
    public bool Running => Elapsed >= 20;
    public float Fade => Mathf.Clamp01((Elapsed - 24) / 1.5f);
    public void Begin(bool replay)
    {
        Elapsed = 0; soundPage=-1; stepAt=20;
        Replay = replay;
        GameFlow.Instance.SetScreen(FlowScreen.Opening);
    }
    void Update()
    {
        if (GameFlow.Instance.Screen != FlowScreen.Opening)
            return;
        Elapsed += Time.unscaledDeltaTime;
        if (!Running && soundPage != Page) { soundPage=Page;GameAudio.Play("story_"+Page,.4f); }
        if (Running && Elapsed<24 && Elapsed>=stepAt) { GameAudio.Play("step_1",.22f);stepAt=Elapsed+.28f; }
        if (Input.GetKeyDown(KeyCode.Escape))
            Finish();
        else if (Input.GetKeyDown(KeyCode.Return) && Elapsed < 20)
            Elapsed = (Page + 1) * 5;
        if (Elapsed >= 26)
            Finish();
    }
    public void Finish()
    {
        if (GameFlow.Instance.Screen != FlowScreen.Opening)
            return;
        PlayerPrefs.SetInt("Fit.StorySeen", 1);
        PlayerPrefs.Save();
        if (Replay)
            GameFlow.Instance.SetScreen(FlowScreen.Menu);
        else
            GameFlow.Instance.StartStage(1);
    }
}
