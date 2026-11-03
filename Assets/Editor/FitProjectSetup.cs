using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using MCPForUnity.Editor.Services;

[InitializeOnLoad]
public static class FitProjectSetup
{
    [MenuItem("Fit to Pass/Prepare campaign")]
    public static void Prepare()
    {
        PlayerSettings.companyName = "Yibo Wang";
        PlayerSettings.productName = "Fit to Pass";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
        foreach (var file in Directory.GetFiles("Assets/Resources/Generated", "*.png"))
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
        var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().backgroundColor = new Color(.025f, .055f, .075f);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(menu, "Assets/Scenes/MainMenu.unity");
        EditorBuildSettings.scenes = new[] { "MainMenu" }.Concat(Enumerable.Range(1, CampaignMap.Count).Select(CampaignMap.Scene)).Select(n => new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true)).ToArray();
        AssetDatabase.SaveAssets();
        Audit();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
    static FitProjectSetup()
    {
        ConfigureLocalMcpEnvironment();
        EditorApplication.delayCall += Connect;
    }
    static void ConfigureLocalMcpEnvironment()
    {
        if (Application.isBatchMode) return;
        // Hub inherits its environment when it starts. Resolve the project's own
        // runtime so an already-running Hub cannot leave Python undiscoverable.
        var scripts = Path.GetFullPath(Path.Combine(Application.dataPath, "../.local/mcp-env/Scripts"));
        if (!File.Exists(Path.Combine(scripts, "python.exe"))) return;
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        if (!path.Split(Path.PathSeparator).Any(p => string.Equals(p, scripts, StringComparison.OrdinalIgnoreCase)))
            Environment.SetEnvironmentVariable("PATH", scripts + Path.PathSeparator + path);
        if (File.Exists(Path.Combine(scripts, "uvx.exe")))
            EditorPrefs.SetString("MCPForUnity.UvxPath", Path.Combine(scripts, "uvx.exe"));
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
    }
    [MenuItem("Fit to Pass/Connect MCP")]
    public static async void Connect()
    {
        if (Application.isBatchMode || EditorApplication.isCompiling)
            return;
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8088");
        foreach (var tool in MCPServiceLocator.ToolDiscovery.DiscoverAllTools())
            MCPServiceLocator.ToolDiscovery.SetToolEnabled(tool.Name, true);
        await MCPServiceLocator.Bridge.StartAsync();
    }

    [MenuItem("Fit to Pass/Audit original scenes")]
    public static void Audit()
    {
        Directory.CreateDirectory(".local/review");
        var report = new System.Text.StringBuilder();
        foreach (string level in new[] { "Level1", "Level2", "Level3" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + level + ".unity");
            report.AppendLine("\nSCENE " + level);
            foreach (var go in scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)))
            {
                var components = go.GetComponents<Component>();
                if (components.Any(x => x == null))
                    report.AppendLine("MISSING SCRIPT " + go.name);
                if (components.Any(x => x is Collider2D || x is PlayerController || x is Tilemap || x is Camera))
                    report.AppendLine(go.name + " | " + go.position + " | scale " + go.lossyScale + " | " + string.Join(",", components.Where(x => x != null).Select(x => x.GetType().Name)));
                var map = go.GetComponent<Tilemap>();
                if (map != null)
                {
                    report.AppendLine("TILES bounds " + map.cellBounds + " grid " + map.layoutGrid.cellSize);
                    using (var writer = new StreamWriter(".local/review/" + level + "-" + go.name + "-tiles.csv"))
                    {
                        writer.WriteLine("x,y,worldX,worldY,tile");
                        foreach (var pos in map.cellBounds.allPositionsWithin)
                            if (map.HasTile(pos))
                            {
                                var world = map.GetCellCenterWorld(pos);
                                writer.WriteLine($"{pos.x},{pos.y},{world.x},{world.y},{map.GetTile(pos).name}");
                            }
                    }
                }
            }
        }
        File.WriteAllText(".local/review/scene-audit.txt", report.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        Debug.Log("Fit to Pass scene audit complete.");
    }
}
