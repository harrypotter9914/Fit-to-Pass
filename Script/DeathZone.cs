using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 用于加载场景

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检测触发对象是否是玩家并且死亡区域具有 "Death" Tag
        if (other.CompareTag("Player") && CompareTag("Death"))
        {
            // 重新加载当前关卡
            ReloadCurrentScene();
        }
    }

    private void ReloadCurrentScene()
    {
        // 获取当前场景的名称
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 重新加载当前场景
        SceneManager.LoadScene(currentSceneName);
    }
}
