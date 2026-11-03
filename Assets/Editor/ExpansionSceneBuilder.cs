using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ExpansionSceneBuilder
{
    [MenuItem("Fit to Pass/Create missing campaign scenes")]
    public static void Create()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop play mode first.");
        for (int stage = 1; stage <= CampaignMap.Count; stage++)
        {
            if (CampaignMap.Original(stage)) continue;
            string name = CampaignMap.Scene(stage);
            if (System.IO.File.Exists("Assets/Scenes/" + name + ".unity")) continue;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 3, -10);
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 5.4f;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CampaignPlayer.prefab"));
            player.transform.position = new Vector3(0, 2, 0);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
        }
        EditorBuildSettings.scenes = new[] { "MainMenu" }.Concat(Enumerable.Range(1, CampaignMap.Count).Select(CampaignMap.Scene))
            .Select(n => new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true)).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
}
