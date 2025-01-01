using UnityEngine;

public class ButtonController : MonoBehaviour
{
    public Transform buttonTransform; // 按钮的位置（如果需要显示）
    public int activationWeightIndex = 12; // 激活按钮所需的体重格子索引
    public PlatformController platformController; // 引用升降平台控制器
    public Animator buttonAnimator; // 按钮的动画控制器

    private bool isActivated = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查碰撞的对象是否是玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && player.GetCurrentWeightIndex() >= activationWeightIndex)
            {
                ActivateButton();
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // 检查碰撞的对象是否是玩家
        if (collision.gameObject.CompareTag("Player"))
        {
            DeactivateButton();
        }
    }

    void ActivateButton()
    {
        if (!isActivated)
        {
            isActivated = true;
            platformController.ActivatePlatform();

            // 激活按钮动画
            if (buttonAnimator != null)
            {
                buttonAnimator.SetBool("Ispressed", true);
            }
        }
    }

    void DeactivateButton()
    {
        if (isActivated)
        {
            isActivated = false;
            platformController.DeactivatePlatform();

            // 可选：你可以在这里添加动画来恢复按钮的状态
            // buttonAnimator.SetTrigger("Idle");  // 假设你有一个恢复按钮的动画
        }
    }
}
