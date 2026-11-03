using UnityEngine;

/// <summary>
/// A laboratory balance servo makes the three mass states readable. Contact with
/// the left half commands an angle; the right counterweight remains physical.
/// The motor releases when the subject leaves, so the bridge responds naturally.
/// </summary>
[RequireComponent(typeof(HingeJoint2D), typeof(Rigidbody2D))]
public class BalanceBridge : MonoBehaviour
{
    HingeJoint2D hinge;
    PlayerController rider;
    public int riderSide = -1;

    void Awake()
    {
        hinge = GetComponent<HingeJoint2D>();
    }
    void FixedUpdate()
    {
        if (GameFlow.InputBlocked)
            return;
        bool engaged = rider && (rider.transform.position.x-transform.position.x)*riderSide > 0;
        hinge.useMotor = engaged;
        if (!engaged)
            return;
        float target = (rider.Body.mass - 1) * 24 * -riderSide;
        // Joint angle has the opposite sign to the body's world rotation.
        hinge.motor = new JointMotor2D { motorSpeed = Mathf.Clamp(Mathf.DeltaAngle(hinge.jointAngle, -target) * 5, -30, 30), maxMotorTorque = 150 };
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player && player.transform.position.y > transform.position.y)
            rider = player;
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>() == rider)
            rider = null;
    }
}
