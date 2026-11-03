using UnityEngine;
/// <summary>Continuous, interruptible counterweight motion, with explicit size state rather than transform sign.</summary>
public class PlatformAndRockController : MonoBehaviour
{
    public Transform platformTransform, rockTransform;
    public Vector3 platformDownPosition, platformUpPosition, rockUpPosition, rockDownPosition;
    public float platformMoveSpeed = 2, rockMoveSpeed = 1.5f;
    public PlayerController player;
    PlayerController rider;
    void Awake()
    {
        if (!platformTransform)
            platformTransform = transform;
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked || !rockTransform)
            return;
        bool lower = rider && rider.Size == 2;
        // An empty plate starts returning immediately, at the authored speeds.
        // The falling crate creates the timed dash; there is no idle grace period.
        // Heavy lowers the platform, light raises it, and medium balances it
        // at its current height. Re-evaluate every tick so eating can reverse
        // a moving mechanism without leaving and landing on it again.
        if (rider && rider.Size == 1)
            return;
        Vector3 old = platformTransform.position;
        platformTransform.position = Vector3.MoveTowards(old, lower ? platformDownPosition : platformUpPosition, platformMoveSpeed * Time.fixedDeltaTime);
        rockTransform.position = Vector3.MoveTowards(rockTransform.position, lower ? rockUpPosition : rockDownPosition, rockMoveSpeed * Time.fixedDeltaTime);
        if (rider && rider.Body.linearVelocity.y <= .2f)
            rider.Body.position += (Vector2)(platformTransform.position - old);
    }
    void OnCollisionStay2D(Collision2D c)
    {
        var p = c.gameObject.GetComponent<PlayerController>();
        if (p && p.transform.position.y > platformTransform.position.y)
            rider = p;
    }
    void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.GetComponent<PlayerController>() == rider)
            rider = null;
    }
}
