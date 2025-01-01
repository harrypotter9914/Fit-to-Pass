using UnityEngine;

public class PlatformController : MonoBehaviour
{
    public Transform platformTransform; // 升降平台的位置
    public Vector3 downPosition; // 平台下方的位置
    public Vector3 upPosition; // 平台上方的位置
    public float speed = 2f; // 平台移动速度

    private bool isMovingDown = false;
    private bool isMovingUp = false;
    private bool isOnPlatform = false; // 检查角色是否在平台上
    private bool isAtBottom = false; // 检查平台是否到达底部

    void Update()
    {
        if (isMovingDown)
        {
            MovePlatformDown();
        }
        else if (isMovingUp)
        {
            MovePlatformUp();
        }
    }

    public void ActivatePlatform()
    {
        if (!isMovingDown && !isMovingUp)
        {
            isMovingDown = true;
        }
    }

    public void DeactivatePlatform()
    {
        isMovingDown = false;
        isMovingUp = false;
    }

    void MovePlatformUp()
    {
        platformTransform.position = Vector3.MoveTowards(platformTransform.position, upPosition, speed * Time.deltaTime);
        if (platformTransform.position == upPosition)
        {
            isMovingUp = false;
            Debug.Log("Platform reached up position.");
        }
    }

    void MovePlatformDown()
    {
        platformTransform.position = Vector3.MoveTowards(platformTransform.position, downPosition, speed * Time.deltaTime);
        if (platformTransform.position == downPosition)
        {
            isMovingDown = false;
            isAtBottom = true; // 平台到达最下方
            if (isOnPlatform)
            {
                isMovingUp = true; // 如果角色在平台上，开始上升
                Debug.Log("Platform is moving up.");
            }
            else
            {
                Debug.Log("Platform reached down position, but no player on it.");
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isOnPlatform = true; // 角色站在平台上
            Debug.Log("Player entered the platform.");

            if (isAtBottom && !isMovingUp) // 如果平台已到达最下方
            {
                isMovingUp = true; // 开始上升
                Debug.Log("Player is on platform, moving up.");
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            isOnPlatform = false; // 角色离开平台
            Debug.Log("Player exited the platform.");
        }
    }
}
