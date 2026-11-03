using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Samples input per frame and applies movement on fixed physics ticks.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    public float initialMoveSpeed = 5f, initialJumpForce = 10f;
    public Transform groundCheck, wallCheckLeft, wallCheckRight;
    public LayerMask groundLayer;
    public Slider weightBar;
    public Vector3 smallScale = new Vector3(.5f, .5f, 1), normalScale = Vector3.one, largeScale = new Vector3(1.5f, 1.5f, 1);
    public float speedMultiplier = 1f, jumpMultiplier = .6f;
    public TMP_Text junkFoodText, healthyFoodText;
    public float weightIncreaseInterval = 2f, weightDecreaseInterval = 2f;
    public float coyoteTime = .12f, jumpBufferTime = .14f, initialGravityScale = 2.5f, fallMultiplier = 1.65f;
    public int JumpCount { get; private set; }
    public int CollectedFoods { get; private set; }
    public int ConsumedEnergy { get; private set; }
    public int ConsumedFresh { get; private set; }
    public int Weight { get; private set; } = 9;
    public int Size => Weight < 6 ? 0 : Weight < 12 ? 1 : 2;
    public int JunkCount
    {
        get; private set;
    }
    public int HealthyCount
    {
        get; private set;
    }
    public bool Grounded
    {
        get; private set;
    }
    public bool FacingRight { get; private set; } = true;
    public float HorizontalInput
    {
        get; private set;
    }
    public Rigidbody2D Body
    {
        get; private set;
    }
    public bool GrowthBlocked
    {
        get; private set;
    }
    public bool HasNearbyFood => NearestFood() != null;
    private Collider2D bodyCollider;
    private SpriteRenderer sprite;
    private readonly HashSet<Collider2D> food = new HashSet<Collider2D>();
    private float jumpBuffer, coyote, metabolism, airborneLock;
    private bool jumpHeld, wasMoving, fullLaunch;
    private Vector2 previousPosition;
    private static PhysicsMaterial2D movementMaterial;
#if UNITY_EDITOR
    public bool TestInputActive, TestJumpHeld, TestJumpPressed;
    public float TestHorizontal;
#endif
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private readonly Collider2D[] overlaps = new Collider2D[16];

    void Awake()
    {
        Body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        Body.freezeRotation = true;
        Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (!movementMaterial)
            movementMaterial = new PhysicsMaterial2D("Frictionless player movement") { friction = 0, bounciness = 0, frictionCombine = PhysicsMaterialCombine2D.Minimum };
        bodyCollider.sharedMaterial = movementMaterial;
        if (groundLayer.value == 0)
            groundLayer = 1 << 6;
        if (weightBar)
        {
            weightBar.maxValue = 17;
            weightBar.value = Weight;
        }
        ApplySize(1);
        previousPosition = Body.position;
        UpdateFoodUI();
    }
    void Update()
    {
        if (GameFlow.InputBlocked)
        {
            HorizontalInput = 0;
            jumpBuffer = 0;
            return;
        }
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        jumpHeld = Input.GetButton("Jump");
        if (Input.GetButtonDown("Jump"))
            jumpBuffer = jumpBufferTime;
#if UNITY_EDITOR
        if (TestInputActive)
        {
            HorizontalInput = TestHorizontal;
            jumpHeld = TestJumpHeld;
            if (TestJumpPressed)
            {
                jumpBuffer = jumpBufferTime;
                TestJumpPressed = false;
            }
        }
#endif
        if (HorizontalInput != 0)
            FacingRight = HorizontalInput > 0;
        if (sprite)
            sprite.flipX = !FacingRight;
        if (Input.GetKeyDown(KeyCode.F))
            Collect();
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            Consume(true);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            Consume(false);
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked)
            return;
        float dt = Time.fixedDeltaTime;
        bool moving = Mathf.Abs(Body.position.x - previousPosition.x) > dt * .2f && Mathf.Abs(HorizontalInput) > .1f;
        previousPosition = Body.position;
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayer, useTriggers = false };
        int count = bodyCollider.Cast(Vector2.down, filter, groundHits, .065f);
        Grounded = false;
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > .55f && groundHits[i].collider.attachedRigidbody != Body)
                Grounded = true;
        airborneLock = Mathf.Max(0, airborneLock - dt);
        if (airborneLock > 0)
            Grounded = false;
        coyote = Grounded ? coyoteTime : Mathf.Max(0, coyote - dt);
        jumpBuffer = Mathf.Max(0, jumpBuffer - dt);
        float speed = initialMoveSpeed * (Size == 0 ? 2f : Size == 2 ? .5f : 1f);
        Body.linearVelocity = new Vector2(HorizontalInput * speed, Body.linearVelocity.y);
        if (jumpBuffer > 0 && coyote > 0)
        {
            JumpCount++;
            Launch(initialJumpForce * (Size == 0 ? 1.3f : Size == 2 ? .7f : 1f), false);
            GameAudio.At("jump_" + Size, transform.position, .4f);
        }
        if (Grounded)
            fullLaunch = false;
        Body.gravityScale = initialGravityScale * (Body.linearVelocity.y < -.1f ? fallMultiplier : (!fullLaunch && !jumpHeld && Body.linearVelocity.y > .1f ? 1.5f : 1f));
        if (moving != wasMoving)
        {
            metabolism = 0;
            wasMoving = moving;
        }
        metabolism += dt;
        float interval = Mathf.Max(.1f, moving ? weightDecreaseInterval : weightIncreaseInterval);
        if (metabolism >= interval)
        {
            metabolism = 0;
            ChangeWeight(moving ? -1 : 1);
        }
        if (transform.position.y < -100)
            GameFlow.Instance?.Die();
    }
    public void Launch(float velocity, bool fullHeight = true)
    {
        Body.linearVelocity = new Vector2(Body.linearVelocity.x, velocity);
        coyote = jumpBuffer = 0;
        airborneLock = .16f;
        Grounded = false;
        fullLaunch = fullHeight;
    }
    public bool ChangeWeight(int amount, bool notifyBlocked = true)
    {
        var hold = GetComponent<MassLock>();
        if (hold && hold.Locked && !hold.Applying)
        {
            hold.Buffer(amount);
            return true;
        }
        int target = Mathf.Clamp(Weight + amount, 0, 17);
        int newSize = target < 6 ? 0 : target < 12 ? 1 : 2;
        GrowthBlocked = newSize > Size && !CanFit(newSize);
        if (GrowthBlocked)
        {
            if (notifyBlocked) { GameFlow.Instance?.Toast("Not enough room to grow. Move into open space."); GameAudio.At("growth_blocked", transform.position, .25f); }
            return false;
        }
        int oldSize = Size;
        Weight = target;
        if (oldSize != newSize)
        {
            ApplySize(newSize);
            GameAudio.At(newSize > oldSize ? "shift_up" : "shift_down", transform.position, .45f);
        }
        if (weightBar)
            weightBar.value = Weight;
        return true;
    }
    bool CanFit(int size)
    {
        Bounds b = bodyCollider.bounds;
        float ratio = ScaleFor(size).y / Mathf.Abs(transform.localScale.y);
        Vector2 desired = b.size * ratio;
        Vector2 center = new Vector2(b.center.x, b.min.y + desired.y * .5f + .035f);
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayer, useTriggers = false };
        int n = Physics2D.OverlapBox(center, desired - new Vector2(.06f, .07f), 0, filter, overlaps);
        for (int i = 0; i < n; i++)
            if (overlaps[i] && overlaps[i].attachedRigidbody != Body)
                return false;
        return true;
    }
    Vector3 ScaleFor(int size) => size == 0 ? smallScale : size == 2 ? largeScale : normalScale;
    void ApplySize(int size)
    {
        float bottom = bodyCollider.bounds.min.y;
        transform.localScale = ScaleFor(size);
        Physics2D.SyncTransforms();
        transform.position += Vector3.up * (bottom - bodyCollider.bounds.min.y + .015f);
        Body.mass = size == 0 ? .5f : size == 2 ? 1.5f : 1f;
    }
    Collider2D NearestFood()
    {
        Collider2D result = null;
        float distance = float.MaxValue;
        food.RemoveWhere(x => !x);
        foreach (var item in food)
        {
            float d = ((Vector2)item.transform.position - Body.position).sqrMagnitude;
            if (d < distance)
            {
                result = item;
                distance = d;
            }
        }
        return result;
    }
    void Collect()
    {
        var item = NearestFood();
        if (!item)
            return;
        int amount = Mathf.Max(1, item.GetComponent<FoodVisual>() ? item.GetComponent<FoodVisual>().quantity : 1);
        if (item.CompareTag("HealthyFood"))
            HealthyCount += amount;
        else
            JunkCount += amount;
        CollectedFoods++;
        food.Remove(item);
        Destroy(item.gameObject);
        UpdateFoodUI();
        GameAudio.At(item.CompareTag("HealthyFood") ? "pickup_fresh" : "pickup_gain", transform.position, .5f);
    }
    public void Consume(bool gain)
    {
        if ((gain ? JunkCount : HealthyCount) <= 0)
        {
            GameFlow.Instance?.Toast(gain ? "Out of energy food. Rest in open space to gain mass." : "Out of fresh food. Keep moving to shed mass.");
            GameAudio.At("food_empty", transform.position, .3f);
            return;
        }
        if (!ChangeWeight(gain ? 2 : -2))
            return;
        if (gain)
            JunkCount--;
        else
            HealthyCount--;
        UpdateFoodUI();
        if (gain) ConsumedEnergy++; else ConsumedFresh++;
        GameAudio.At(gain ? "eat_gain" : "eat_fresh", transform.position, .45f);
    }
    public void Restore(int weight, int gainFood, int lightFood)
    {
        GetComponent<MassLock>()?.Clear();
        GrowthBlocked = false;
        Weight = Mathf.Clamp(weight, 0, 17);
        JunkCount = gainFood;
        HealthyCount = lightFood;
        metabolism = coyote = jumpBuffer = 0;
        food.Clear();
        ApplySize(Size);
        Body.linearVelocity = Vector2.zero;
        previousPosition = Body.position;
        UpdateFoodUI();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HealthyFood") || other.CompareTag("JunkFood"))
            food.Add(other);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        food.Remove(other);
    }
    void UpdateFoodUI()
    {
        if (junkFoodText)
            junkFoodText.text = "Energy  " + JunkCount;
        if (healthyFoodText)
            healthyFoodText.text = "Fresh  " + HealthyCount;
    }
    public int GetCurrentWeightIndex() => Weight;
}
