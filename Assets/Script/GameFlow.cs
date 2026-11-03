using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FlowScreen
{
    Menu, Stages, Intro, Playing, Paused, Settings, Dead, Complete, Ending, Credits, Tutorial, Opening, NewGameConfirm, Restoring, Discovery
}

/// <summary>Single owner of scene transitions, pause state, campaign progress and checkpoint snapshots.</summary>
public class GameFlow : MonoBehaviour
{
    public static GameFlow Instance
    {
        get; private set;
    }
    public static bool InputBlocked => Instance && Instance.Screen != FlowScreen.Playing;
    public FlowScreen Screen { get; private set; } = FlowScreen.Menu;
    public FlowScreen PreviousScreen
    {
        get; private set;
    }
    public PlayerController Player
    {
        get; private set;
    }
    public int Stage
    {
        get; private set;
    }
    public float Elapsed
    {
        get; private set;
    }
    public int Deaths
    {
        get; private set;
    }
    public bool Loading
    {
        get; private set;
    }
    public const int ChapterCount = CampaignMap.Count;
    public bool InAnnex
    {
        get; private set;
    }
    public int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt("Fit.Unlocked", 1), 1, ChapterCount);
    public bool HasProgress => PlayerPrefs.GetInt("Fit.HasRun", 0) != 0 || Unlocked > 1 || PlayerPrefs.GetInt("Fit.StorySeen", 0) != 0;
    public int ContinueStage => Mathf.Clamp(PlayerPrefs.GetInt("Fit.ResumeStage", Unlocked), 1, Unlocked);
    public const int MinimumScore = 1000;
    public int Score => Mathf.Max(0, 10000 - Mathf.RoundToInt(Elapsed * (CampaignMap.Original(Stage) ? 12 : 3)));
    public bool ScoreFailed
    {
        get; private set;
    }
    public string Notice
    {
        get; private set;
    }
    public float NoticeUntil
    {
        get; private set;
    }
    public static readonly string[] StageNames = CampaignMap.Names;
    public static readonly string[] Briefings = CampaignMap.Briefings;
    CheckpointSnapshot checkpoint;
    bool restoring;
    FlowScreen settingsReturn = FlowScreen.Menu;
    class CheckpointSnapshot
    {
        public Vector3 position; public int weight, gain, light; public HashSet<string> remaining; public CampaignCheckpointState.Device[] devices;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (!Instance)
            new GameObject("Fit to Pass - Campaign").AddComponent<GameFlow>();
    }
    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CampaignMap.MigrateProgress();
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<GameAudio>();
        gameObject.AddComponent<GameHUD>();
        gameObject.AddComponent<FirstEncounterGuide>();
        gameObject.AddComponent<OpeningSequence>();
        AudioListener.volume = PlayerPrefs.GetFloat("Fit.Volume", .65f);
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        Application.runInBackground = true;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1;
        }
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int chapter = CampaignMap.StageForScene(scene.name);
        if (chapter == 0)
        {
            Stage = 0;
            Player = null;
            SetScreen(FlowScreen.Menu);
            return;
        }
        Stage = chapter;
        TutorialDirector.Reset();
        Player = FindAnyObjectByType<PlayerController>();
        if (!Player)
        {
            Debug.LogError("Campaign scene is missing a player: " + scene.name);
            SetScreen(FlowScreen.Menu);
            return;
        }
        GamePresentation.Apply(Stage, Player);
        DeviceSound.Install();
        Player.initialMoveSpeed = 5;
        Player.initialJumpForce = 9.6f;
        Player.initialGravityScale = 1.8f;
        Player.fallMultiplier = 1.5f;
        int climate = CampaignMap.Region(Stage);
        Player.weightIncreaseInterval = climate == 1 ? 1 : climate == 2 ? 3 : 2;
        Player.weightDecreaseInterval = climate == 1 ? 3 : climate == 2 ? 1 : 2;
        if (restoring && checkpoint != null)
        {
            Player.Restore(checkpoint.weight, checkpoint.gain, checkpoint.light);
            Player.transform.position = checkpoint.position;
            foreach (var pickup in FindObjectsByType<FoodVisual>())
                if (!checkpoint.remaining.Contains(FoodKey(pickup.transform)))
                    Destroy(pickup.gameObject);
            Player.GetComponent<CameraFollow>()?.Snap();
            SetScreen(FlowScreen.Restoring);
            StartCoroutine(RestoreDevices(checkpoint.devices));
        }
        else
        {
            Player.Restore(9, 2, 2);
            SetScreen(FlowScreen.Playing);
        }
        if (!restoring)
        {
            Player.GetComponent<CameraFollow>()?.Snap();
            EnvironmentalGuidance.BeginStage(Stage, Player);
            FirstEncounterGuide.Instance.BeginStage(Stage,Player);
        }
        restoring = false;
        Loading = false;
    }
    IEnumerator RestoreDevices(CampaignCheckpointState.Device[] devices)
    {
        yield return null; // Device Start methods must create their visual and collision state first.
        CampaignCheckpointState.Restore(devices);
        Physics2D.SyncTransforms();
        if (RespawnSafety.FromSavedRoot(Player, checkpoint.position, Player.Size, out var safe))
            Player.Body.position = safe;
        Player.Body.linearVelocity = Vector2.zero;
        Player.GetComponent<CameraFollow>()?.Snap();
        SetScreen(FlowScreen.Playing);
        EnvironmentalGuidance.BeginStage(Stage, Player, true);
        FirstEncounterGuide.Instance.BeginStage(Stage,Player);
    }
    void Update()
    {
        if (Screen == FlowScreen.Playing)
        {
            Elapsed += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape))
                SetScreen(FlowScreen.Paused);
            else if (Input.GetKeyDown(KeyCode.R))
                Die();
        }
        else if (Screen == FlowScreen.Paused && Input.GetKeyDown(KeyCode.Escape))
            SetScreen(FlowScreen.Playing);
        else if (Screen == FlowScreen.Intro && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            BeginGameplay();
        else if (Screen == FlowScreen.Dead && Input.GetKeyDown(KeyCode.Return))
            Retry();
        else if (Screen == FlowScreen.Tutorial && Input.GetKeyDown(KeyCode.Return))
            TutorialDirector.Advance();
        else if (Screen == FlowScreen.NewGameConfirm && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return)))
            CancelNewGame();
    }
    void OnApplicationFocus(bool focused)
    {
        if (!focused && Screen == FlowScreen.Playing)
            SetScreen(FlowScreen.Paused);
    }
    public void SetScreen(FlowScreen screen)
    {
        PreviousScreen = Screen;
        Screen = screen;
        Time.timeScale = screen == FlowScreen.Playing ? 1 : 0;
        GameAudio.ScreenChanged(PreviousScreen, screen);
    }
    public void StartStage(int stage)
    {
        if (Loading)
            return;
        Stage = Mathf.Clamp(stage, 1, ChapterCount);
        PlayerPrefs.SetInt("Fit.HasRun", 1);
        PlayerPrefs.SetInt("Fit.ResumeStage", Mathf.Min(Stage, Unlocked));
        PlayerPrefs.Save();
        InAnnex = false;
        Elapsed = 0;
        Deaths = 0;
        ScoreFailed = false;
        checkpoint = null;
        restoring = false;
        StartCoroutine(Load(CampaignMap.Scene(Stage)));
    }
    IEnumerator Load(string scene)
    {
        Loading = true;
        Time.timeScale = 1;
        var op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone)
            yield return null;
        Loading = false;
    }
    public void Resume()
    {
        SetScreen(FlowScreen.Playing);
    }
    public void BeginGameplay()
    {
        Resume();
    }
    public void BeginCampaign(int stage)
    {
        if (stage == 1 && PlayerPrefs.GetInt("Fit.StorySeen", 0) == 0)
            GetComponent<OpeningSequence>().Begin(false);
        else
            StartStage(stage);
    }
    public void RequestNewGame()
    {
        if (Loading || Screen != FlowScreen.Menu) return;
        if (HasProgress) SetScreen(FlowScreen.NewGameConfirm);
        else StartFreshCampaign();
    }
    public void CancelNewGame()
    {
        if (Screen == FlowScreen.NewGameConfirm) SetScreen(FlowScreen.Menu);
    }
    public void ConfirmNewGame()
    {
        if (!Loading && Screen == FlowScreen.NewGameConfirm) StartFreshCampaign();
    }
    void StartFreshCampaign()
    {
        // Only this campaign's progress is reset. Audio/display preferences survive.
        foreach (string key in new[] { "Fit.Unlocked", "Fit.ResumeStage", "Fit.StorySeen", "Fit.HasRun" })
            PlayerPrefs.DeleteKey(key);
        for (int i = 1; i <= ChapterCount; i++)
        {
            PlayerPrefs.DeleteKey("Fit.Best." + i);
            PlayerPrefs.DeleteKey("Fit.Completed." + i);
        }
        PlayerPrefs.SetInt("Fit.CampaignVersion", CampaignMap.Version);
        TutorialDirector.ClearProgress();
        checkpoint = null;
        restoring = InAnnex = ScoreFailed = false;
        Elapsed = Deaths = 0;
        NoticeUntil = 0;
        PlayerPrefs.SetInt("Fit.HasRun", 1);
        PlayerPrefs.SetInt("Fit.ResumeStage", 1);
        PlayerPrefs.Save();
        GetComponent<OpeningSequence>().Begin(false);
    }
    public void EnterAnnex() => Finish(); // Compatibility with old editor scripts; no side-room teleport.
    public void Die()
    {
        if (Screen != FlowScreen.Playing)
            return;
        Deaths++;
        GameAudio.Play("death", .4f);
        SetScreen(FlowScreen.Dead);
    }
    public void Retry()
    {
        if (Loading)
            return;
        if (ScoreFailed)
        {
            StartStage(Stage);
            return;
        }
        restoring = checkpoint != null;
        StartCoroutine(Load(CampaignMap.Scene(Stage)));
    }
    public void Finish()
    {
        if (Screen != FlowScreen.Playing)
            return;
        if (CampaignMap.Original(Stage) && Score < MinimumScore)
        {
            ScoreFailed = true;
            GameAudio.Play("death", .4f);
            SetScreen(FlowScreen.Dead);
            return;
        }
        PlayerPrefs.SetInt("Fit.Unlocked", Mathf.Max(Unlocked, Mathf.Min(ChapterCount, Stage + 1)));
        PlayerPrefs.SetInt("Fit.Best." + Stage, Mathf.Max(Score, PlayerPrefs.GetInt("Fit.Best." + Stage, 0)));
        PlayerPrefs.SetInt("Fit.Completed." + Stage, 1);
        PlayerPrefs.SetInt("Fit.ResumeStage", Mathf.Min(ChapterCount, Stage + 1));
        PlayerPrefs.Save();
        GameAudio.Play("complete", .6f);
        SetScreen(FlowScreen.Complete);
    }
    public void Next()
    {
        if (Stage < ChapterCount)
            StartStage(Stage + 1);
        else
            SetScreen(FlowScreen.Ending);
    }
    public void MainMenu()
    {
        if (!Loading)
            StartCoroutine(Load("MainMenu"));
    }
    public void OpenSettings()
    {
        settingsReturn = Screen;
        SetScreen(FlowScreen.Settings);
    }
    public void CloseSettings()
    {
        SetScreen(settingsReturn);
    }
    public void Toast(string message)
    {
        Notice = message;
        NoticeUntil = Time.unscaledTime + 3;
    }
    public void Checkpoint(Vector3 position)
    {
        if (!Player || Screen != FlowScreen.Playing)
            return;
        checkpoint = new CheckpointSnapshot { position = position, weight = Player.Weight, gain = Player.JunkCount, light = Player.HealthyCount, remaining = new HashSet<string>(), devices = CampaignCheckpointState.Capture() };
        foreach (var pickup in FindObjectsByType<FoodVisual>())
            checkpoint.remaining.Add(FoodKey(pickup.transform));
        Toast("Checkpoint saved");
        GameAudio.Play("checkpoint", .4f);
    }
    static string FoodKey(Transform t) => t.GetComponent<FoodVisual>().PersistentId;
}
