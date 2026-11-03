using UnityEngine;
/// <summary>Call the lift with a heavy pressure plate, then ride it back to the upper stop.</summary>
public class PlatformController : MonoBehaviour
{
    public Transform platformTransform;
    public Vector3 downPosition, upPosition;
    public float speed = 2;
    enum LiftState
    {
        Idle, Lowering, Waiting, Raising
    }
    LiftState state;
    PlayerController rider;
    void Awake()
    {
        if (!platformTransform)
            platformTransform = transform;
    }
    public void ActivatePlatform()
    {
        if (state == LiftState.Idle)
            state = LiftState.Lowering;
    }
    public void DeactivatePlatform()
    {
    } // A completed button press latches the call; stepping off cannot strand the lift.
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked)
            return;
        if (state == LiftState.Waiting && rider)
            state = LiftState.Raising;
        if (state != LiftState.Lowering && state != LiftState.Raising)
            return;
        Vector3 target = state == LiftState.Lowering ? downPosition : upPosition;
        Vector3 old = platformTransform.position;
        platformTransform.position = Vector3.MoveTowards(old, target, speed * Time.fixedDeltaTime);
        if (rider && rider.Body.linearVelocity.y <= .2f)
            rider.Body.position += (Vector2)(platformTransform.position - old);
        if ((platformTransform.position - target).sqrMagnitude < .0001f)
            state = state == LiftState.Lowering ? LiftState.Waiting : LiftState.Idle;
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
