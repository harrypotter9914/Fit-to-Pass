using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float initialMoveSpeed = 5f;
    public float initialJumpForce = 10f;
    private float moveSpeed;
    private float jumpForce;

    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool isTouchingWallLeft;
    private bool isTouchingWallRight;

    // 体型和体重条参数
    public Slider weightBar;
    private int weightSections = 18;
    private int currentWeightIndex = 9; // 当前体重格子索引（初始在中间）
    public Vector3 smallScale = new Vector3(0.5f, 0.5f, 1f); // 小体型
    public Vector3 normalScale = new Vector3(1f, 1f, 1f); // 中体型
    public Vector3 largeScale = new Vector3(1.5f, 1.5f, 1f); // 大体型

    private Rigidbody2D rb;
    private Animator animator;
    private bool isJumping;

    public float speedMultiplier = 0.2f;
    public float jumpMultiplier = 0.2f;

    private bool isFacingRight = true;

    // 背包管理
    private int junkFoodCount = 0;
    private int healthyFoodCount = 0;
    public TMP_Text junkFoodText;
    public TMP_Text healthyFoodText;

    public float weightIncreaseInterval = 1.5f; // 变大的时间间隔
    public float weightDecreaseInterval = 1f;  // 变小的时间间隔

    private float weightIncreaseTimer = 0f;    // 用于记录体重增加的计时
    private float weightDecreaseTimer = 0f;    // 用于记录体重减少的计时

    private Collider2D nearbyFood; // 保存玩家接触的食物
    private int currentSize = 1; // 0: 小, 1: 中, 2: 大

    // Jump mechanics related variables
    private bool canJump = false;
    private bool isJumpBuffered = false;

    // Coyote Time
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    // Jump Buffer
    public float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;

    // Gravity Modifiers
    public float initialGravityScale = 1f;  // 初始重力
    public float fallMultiplier = 2.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = initialGravityScale;  // 设置初始重力
        animator = GetComponent<Animator>();
        weightBar.maxValue = weightSections - 1;
        weightBar.value = currentWeightIndex;

        moveSpeed = initialMoveSpeed;
        jumpForce = initialJumpForce;

        UpdateScale();
        UpdateFoodUI();
    }

    void Update()
    {
        float moveInput = Input.GetAxis("Horizontal");

        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        isTouchingWallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, 0.2f, groundLayer);
        isTouchingWallRight = Physics2D.OverlapCircle(wallCheckRight.position, 0.2f, groundLayer);

        bool isTouchingWall = isTouchingWallLeft || isTouchingWallRight;

        // Animation related booleans remain unchanged
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWall", isTouchingWall);
        animator.SetBool("isMoving", moveInput != 0 && isGrounded);

        if (rb.velocity.y > 0.1f)
        {
            animator.SetBool("isJumping", true);
            animator.SetBool("isFalling", false);
        }
        else if (rb.velocity.y < -0.1f)
        {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", true);
        }
        else
        {
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
        }

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // Coyote Time logic
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isJumping = false; // Reset jumping state when grounded
        }
        else if (!isJumping) // 只在未跳跃状态下递减
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer logic
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Check if the player can jump
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !isJumping)
        {
            canJump = true;
            isJumpBuffered = true;
            isJumping = true;
            coyoteTimeCounter = 0;
            jumpBufferCounter = 0;
        }

        // Apply jump force
        if (canJump && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            canJump = false;
        }

        // Gravity modifiers for falling
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = fallMultiplier * initialGravityScale;
        }
        else
        {
            rb.gravityScale = initialGravityScale;
        }

        // 处理体重变化逻辑
        if (moveInput != 0)
        {
            weightDecreaseTimer += Time.deltaTime;
            if (weightDecreaseTimer >= weightDecreaseInterval)
            {
                weightDecreaseTimer = 0f;
                ChangeWeight(-1); // 体重减少
            }
        }
        else
        {
            weightIncreaseTimer += Time.deltaTime;
            if (weightIncreaseTimer >= weightIncreaseInterval)
            {
                weightIncreaseTimer = 0f;
                ChangeWeight(1); // 体重增加
            }
        }

        // Check if the player picks up food
        if (Input.GetKeyDown(KeyCode.F) && nearbyFood != null)
        {
            if (nearbyFood.CompareTag("HealthyFood"))
            {
                healthyFoodCount++;
                Destroy(nearbyFood.gameObject);
                UpdateFoodUI();
            }
            else if (nearbyFood.CompareTag("JunkFood"))
            {
                junkFoodCount++;
                Destroy(nearbyFood.gameObject);
                UpdateFoodUI();
            }

            nearbyFood = null;
        }

        // Consume food
        if (Input.GetKeyDown(KeyCode.Alpha1) && junkFoodCount > 0)
        {
            junkFoodCount--;
            UpdateFoodUI();
            ChangeWeight(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && healthyFoodCount > 0)
        {
            healthyFoodCount--;
            UpdateFoodUI();
            ChangeWeight(-2);
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HealthyFood") || other.CompareTag("JunkFood"))
        {
            nearbyFood = other;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == nearbyFood)
        {
            nearbyFood = null;
        }
    }

    void ChangeWeight(int amount)
    {
        currentWeightIndex = Mathf.Clamp(currentWeightIndex + amount, 0, weightSections - 1);
        weightBar.value = currentWeightIndex;

        int newSize = currentWeightIndex < 6 ? 0 : (currentWeightIndex < 12 ? 1 : 2);
        if (newSize != currentSize)
        {
            currentSize = newSize;
            UpdateScale();
        }
    }

    void UpdateScale()
    {
        float originalScaleX = isFacingRight ? 1 : -1;

        if (currentSize == 0) // 小体型
        {
            transform.localScale = new Vector3(originalScaleX * smallScale.x, smallScale.y, smallScale.z);
        }
        else if (currentSize == 1) // 中体型
        {
            transform.localScale = new Vector3(originalScaleX * normalScale.x, normalScale.y, normalScale.z);
        }
        else if (currentSize == 2) // 大体型
        {
            transform.localScale = new Vector3(originalScaleX * largeScale.x, largeScale.y, largeScale.z);
        }

        // 根据体型调整速度和跳跃力
        if (currentSize == 0) // 小体型
        {
            rb.mass = 0.5f; // 小体型质量较轻
            moveSpeed = initialMoveSpeed * (1 + (1f - smallScale.x) * speedMultiplier);
            jumpForce = initialJumpForce * (1 + (1f - smallScale.y) * jumpMultiplier);
        }
        else if (currentSize == 1) // 中体型
        {
            moveSpeed = initialMoveSpeed; // 恢复初始速度和跳跃力
            rb.mass = 1f; // 正常体型质量
            jumpForce = initialJumpForce;
        }
        else if (currentSize == 2) // 大体型
        {
            rb.mass = 1.5f; // 大体型质量较重
            moveSpeed = initialMoveSpeed * (1 - (largeScale.x - 1f) * speedMultiplier);
            jumpForce = initialJumpForce * (1 - (largeScale.y - 1f) * jumpMultiplier);
        }
    }


    void UpdateFoodUI()
    {
        junkFoodText.text = "Junk Food: " + junkFoodCount;
        healthyFoodText.text = "Healthy Food: " + healthyFoodCount;
    }

    public int GetCurrentWeightIndex()
    {
        return currentWeightIndex;
    }
}