using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public float moveSpeed = 2f; // 升降台的移动速度
    private bool isPlayerOnPlatform = false; // 判断玩家是否站在升降台上
    private int currentPlayerSize = 1; // 玩家当前的体型 (0: 小, 1: 中, 2: 大)
    private Transform playerTransform; // 用于存储当前玩家的 Transform

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检测玩家是否站上升降台
        if (collision.collider.CompareTag("Player"))
        {
            PlayerController playerController = collision.collider.GetComponent<PlayerController>();
            isPlayerOnPlatform = true;
            playerTransform = collision.collider.transform; // 保存玩家的 Transform

            if (playerController != null)
            {
                UpdatePlayerSize(playerController); // 更新玩家的体型状态
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 当玩家离开升降台时，重置状态
        if (collision.collider.CompareTag("Player"))
        {
            isPlayerOnPlatform = false;
            playerTransform = null; // 清空玩家的 Transform
        }
    }

    private void Update()
    {
        // 如果玩家在平台上，实时检测玩家体型变化
        if (isPlayerOnPlatform && playerTransform != null)
        {
            PlayerController playerController = playerTransform.GetComponent<PlayerController>();
            if (playerController != null)
            {
                UpdatePlayerSize(playerController); // 动态更新玩家体型
            }

            // 根据玩家体型决定升降台的运动
            if (currentPlayerSize == 0)
            {
                // 小体型：升降台匀速向上移动
                transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
            }
            else if (currentPlayerSize == 2)
            {
                // 大体型：升降台匀速向下移动
                transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
            }
            // 中体型：保持不动（无需操作）
        }
    }

    private void UpdatePlayerSize(PlayerController playerController)
    {
        // 根据玩家的体型动态更新状态
        if (playerController.transform.localScale == playerController.smallScale)
        {
            currentPlayerSize = 0; // 小体型
        }
        else if (playerController.transform.localScale == playerController.normalScale)
        {
            currentPlayerSize = 1; // 中体型
        }
        else if (playerController.transform.localScale == playerController.largeScale)
        {
            currentPlayerSize = 2; // 大体型
        }
    }
}
