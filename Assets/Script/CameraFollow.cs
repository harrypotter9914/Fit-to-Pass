using UnityEngine;
[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Camera m_Camera;
    [SerializeField] float m_SmoothTime = .18f;
    float verticalVelocity;
    PlayerController player;
    Collider2D shape;
    float floorTarget;
    public const float HorizontalDeadZone = .8f;
    void Start()
    {
        m_Camera = Camera.main;
        player = GetComponent<PlayerController>(); shape = GetComponent<Collider2D>();
        if (m_Camera)
            Snap();
    }
    public void Snap()
    {
        if (!m_Camera)
            m_Camera = Camera.main;
        if (!player) player = GetComponent<PlayerController>();
        if (!shape) shape = GetComponent<Collider2D>();
        floorTarget = Feet + RestingEyeHeight;
        if (m_Camera)
            m_Camera.transform.position = new Vector3(transform.position.x, floorTarget, -10);
        verticalVelocity = 0;
    }
    float Feet => shape ? transform.position.y + shape.offset.y * Mathf.Abs(transform.lossyScale.y) - shape.bounds.extents.y : transform.position.y - .71f;
    float RestingEyeHeight => shape is BoxCollider2D box && player ? (box.size.y*.5f-box.offset.y)*Mathf.Abs(player.normalScale.y)+1 : 2.57f;
    void LateUpdate()
    {
        if (!m_Camera || GameFlow.InputBlocked)
            return;
        float dt = Time.deltaTime;
        float desiredY = Feet + RestingEyeHeight;
        // Ground contact and changing body size must not make the whole room bob.
        if (Mathf.Abs(desiredY-floorTarget) > .12f) floorTarget = desiredY;
        var position=m_Camera.transform.position;
        // Follow the interpolated actor only when it leaves the horizontal zone.
        // No input look-ahead, stored horizontal velocity, or automatic recentering:
        // releasing movement must never push the room forward and pull it back.
        position.x=Mathf.Clamp(position.x,transform.position.x-HorizontalDeadZone,transform.position.x+HorizontalDeadZone);
        position.y=Mathf.SmoothDamp(position.y,floorTarget,ref verticalVelocity,Mathf.Clamp(m_SmoothTime,.12f,.18f),Mathf.Infinity,dt);
        position.z=-10;
        m_Camera.transform.position=position;
    }
}
