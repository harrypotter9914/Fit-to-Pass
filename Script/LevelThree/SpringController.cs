using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringController : MonoBehaviour
{
    public Animator springAnimator; // 弹簧的 Animator
    public float weakForce = 5f;    // 弱弹跳的弹跳力
    public float mediumForce = 10f; // 中等弹跳的弹跳力
    public float strongForce = 15f; // 强弹跳的弹跳力

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 检测是否是玩家
        if (collision.collider.CompareTag("Player"))
        {
            PlayerController playerController = collision.collider.GetComponent<PlayerController>();
            Rigidbody2D playerRb = collision.collider.GetComponent<Rigidbody2D>();

            if (playerController != null && playerRb != null)
            {
                // 根据玩家体型播放动画并施加弹跳力
                if (playerController.transform.localScale == playerController.smallScale)
                {
                    springAnimator.SetInteger("SpringState", 0); // 小体型 -> 强弹跳动画
                    ApplyForce(playerRb, strongForce);          // 强弹跳力
                }
                else if (playerController.transform.localScale == playerController.normalScale)
                {
                    springAnimator.SetInteger("SpringState", 1); // 中体型 -> 中弹跳动画
                    ApplyForce(playerRb, mediumForce);          // 中弹跳力
                }
                else if (playerController.transform.localScale == playerController.largeScale)
                {
                    springAnimator.SetInteger("SpringState", 2); // 大体型 -> 弱弹跳动画
                    ApplyForce(playerRb, weakForce);            // 弱弹跳力
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 当玩家离开弹簧时，重置 SpringState 为 3
        if (collision.collider.CompareTag("Player"))
        {
            springAnimator.SetInteger("SpringState", 3);
        }
    }

    private void ApplyForce(Rigidbody2D rb, float force)
    {
        // 清除垂直方向的速度，防止累积之前的跳跃力
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        // 添加垂直向上的弹跳力
        rb.AddForce(new Vector2(0f, force), ForceMode2D.Impulse);
    }
}
