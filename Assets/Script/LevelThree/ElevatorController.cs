using UnityEngine;
public class ElevatorController : MonoBehaviour
{
    public float moveSpeed = 2f, travel = 6f;
    private PlayerController rider; private Vector3 origin;
    void Awake()
    {
        origin = transform.position;
    }
    void FixedUpdate()
    {
        if (!rider || GameFlow.InputBlocked)
            return;
        float direction = rider.Size == 0 ? 1 : rider.Size == 2 ? -1 : 0;
        Vector3 next = transform.position + Vector3.up * (direction * moveSpeed * Time.fixedDeltaTime);
        next.y = Mathf.Clamp(next.y, origin.y - travel, origin.y + travel);
        Vector3 delta = next - transform.position;
        transform.position = next;
        if (rider.Body.linearVelocity.y <= .2f)
            rider.Body.position += (Vector2)delta;
    }
    void OnCollisionStay2D(Collision2D c)
    {
        var p = c.gameObject.GetComponent<PlayerController>();
        if (p && p.transform.position.y > transform.position.y)
            rider = p;
    }
    void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.GetComponent<PlayerController>() == rider)
            rider = null;
    }
}
