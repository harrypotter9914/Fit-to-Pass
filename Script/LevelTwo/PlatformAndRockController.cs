using System.Collections;
using UnityEngine;

public class PlatformAndRockController : MonoBehaviour
{
    public Transform platformTransform;  // 升降台的位置
    public Transform rockTransform;      // 石头的位置
    public Vector3 platformDownPosition; // 升降台的最低点
    public Vector3 platformUpPosition;   // 升降台的最高点
    public Vector3 rockUpPosition;       // 石头的最高点
    public Vector3 rockDownPosition;     // 石头的最低点
    public float platformMoveSpeed = 2f; // 升降台移动速度
    public float rockMoveSpeed = 1.5f;   // 石头移动速度
    public PlayerController player;      // 玩家引用，用于获取体型

    private bool isPlatformMoving = false; // 升降台是否在移动
    private bool playerOnPlatform = false; // 玩家是否站在升降台上
    private bool platformAtBottom = false; // 升降台是否在底部

    void Update()
    {
        // 检查玩家是否为大体型，并且是否站在升降台上
        if (player.GetCurrentWeightIndex() >= 12 && playerOnPlatform)
        {
            // 如果升降台没有到达底部，移动升降台和石头
            if (!platformAtBottom)
            {
                MovePlatformDown();
            }
        }
        else if (!playerOnPlatform && platformAtBottom)
        {
            // 玩家离开且升降台在底部时，上升升降台和下降石头
            MovePlatformUp();
        }
    }

    void MovePlatformDown()
    {
        if (!isPlatformMoving)
        {
            isPlatformMoving = true;
            StartCoroutine(MovePlatformAndRock(platformDownPosition, rockUpPosition, platformMoveSpeed, rockMoveSpeed, false));
        }
    }

    void MovePlatformUp()
    {
        if (!isPlatformMoving)
        {
            isPlatformMoving = true;
            StartCoroutine(MovePlatformAndRock(platformUpPosition, rockDownPosition, platformMoveSpeed, rockMoveSpeed, true));
        }
    }

    // 升降台和石头移动的协程
    IEnumerator MovePlatformAndRock(Vector3 platformTarget, Vector3 rockTarget, float platformSpeed, float rockSpeed, bool movingUp)
    {
        while (Vector3.Distance(platformTransform.position, platformTarget) > 0.01f ||
               Vector3.Distance(rockTransform.position, rockTarget) > 0.01f)
        {
            platformTransform.position = Vector3.MoveTowards(platformTransform.position, platformTarget, platformSpeed * Time.deltaTime);
            rockTransform.position = Vector3.MoveTowards(rockTransform.position, rockTarget, rockSpeed * Time.deltaTime);
            yield return null;
        }

        isPlatformMoving = false;

        if (movingUp)
        {
            platformAtBottom = false;
        }
        else
        {
            platformAtBottom = true;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            playerOnPlatform = true;  // 玩家站在升降台上
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            playerOnPlatform = false;  // 玩家离开升降台
        }
    }
}
