using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;

public static class CampaignTools
{
    [MenuItem("Fit to Pass/Repair legacy scenes")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop play mode before repairing scenes.");
        for (int stage = 1; stage <= 3; stage++)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level" + stage + ".unity");
            foreach (var t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray())
            {
                if (!t)
                    continue;
                if (PrefabUtility.IsPrefabAssetMissing(t.gameObject))
                {
                    Object.DestroyImmediate(t.gameObject);
                    continue;
                }
                if ((stage == 3 && t.position.y > 35 || stage == 2 && t.position.x > 80) && t.GetComponent<SpriteRenderer>())
                    t.gameObject.SetActive(false);
                var body = t.GetComponent<Rigidbody2D>();
                var shape = t.GetComponent<Collider2D>();
                if (stage == 3 && body && body.bodyType == RigidbodyType2D.Dynamic && !t.GetComponent<PlayerController>() && body.constraints != RigidbodyConstraints2D.FreezePosition && !t.GetComponent<HingeJoint2D>())
                {
                    // Equal mass must also have equal gravity for the bridge puzzle.
                    body.gravityScale = 1.8f;
                    body.mass = 1;
                    body.linearDamping = .2f;
                    body.angularDamping = .8f;
                    t.gameObject.layer = 6;
                }
                if (stage == 3 && body && shape && (body.constraints == RigidbodyConstraints2D.FreezePosition || t.GetComponent<HingeJoint2D>()))
                {
                    var hinge = t.GetComponent<HingeJoint2D>();
                    if (!hinge)
                        hinge = t.gameObject.AddComponent<HingeJoint2D>();
                    hinge.autoConfigureConnectedAnchor = false;
                    hinge.connectedAnchor = t.position;
                    hinge.useLimits = true;
                    hinge.limits = new JointAngleLimits2D { min = -12, max = 12 };
                    body.constraints = RigidbodyConstraints2D.None;
                    body.angularDamping = 2;
                    body.mass = 2;
                    if (!t.GetComponent<BalanceBridge>())
                        t.gameObject.AddComponent<BalanceBridge>();
                }
            }
            if (stage == 3)
            {
                var bridges = Object.FindObjectsByType<HingeJoint2D>();
                foreach (var rail in Object.FindObjectsByType<Transform>().Where(t => t.name.StartsWith("ew")))
                    rail.SetParent(bridges.OrderBy(b => (b.transform.position - rail.position).sqrMagnitude).First().transform, true);
            }
            EditorSceneManager.SaveScene(scene);
        }
        var importer = AssetImporter.GetAtPath("Assets/Resources/UI/Interface.ttf") as TrueTypeFontImporter;
        if (importer)
        {
            importer.fontNames = new[] { "Barlow" };
            importer.SaveAndReimport();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Legacy scene references repaired; bridge limits and font configured.");
    }
    [MenuItem("Fit to Pass/Build Windows game")]
    public static void Build()
    {
        // MCP dispatch may run inside UnitySynchronizationContext. Build only on
        // the editor's next delayed callback, outside the player loop.
        EditorApplication.delayCall += BuildNow;
    }
    static void BuildNow()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Generated/AppIcon.png");
        if (icon)
        {
            var sizes = PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Standalone, IconKind.Any);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Standalone, sizes.Select(_ => icon).ToArray(), IconKind.Any);
        }
        Directory.CreateDirectory("Builds/FitToPass");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/FitToPass/FitToPass.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/BUILD-RESULT.txt", report.summary.result + "\nUnity " + Application.unityVersion + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize + "\nDuration: " + report.summary.totalTime);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + report.summary.result);
    }
}
