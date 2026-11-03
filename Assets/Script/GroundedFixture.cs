using UnityEngine;

/// <summary>Records the actual support surface for placement audits (not an animation constraint).</summary>
public class GroundedFixture : MonoBehaviour
{
    public float supportY;
    Transform supportFrame;
    Vector3 localContact;
    public void FollowSupport(Transform frame,Vector3 contact)
    {
        supportFrame=frame;localContact=frame.InverseTransformPoint(contact);
    }
    public float BottomError
    {
        get
        {
            var r=GetComponent<SpriteRenderer>();
            if(!supportFrame)return r.bounds.min.y-supportY;
            var shape=r.sprite.bounds;
            var foot=transform.TransformPoint(new Vector3(shape.center.x,shape.min.y,shape.center.z));
            return Vector3.Dot(foot-supportFrame.TransformPoint(localContact),supportFrame.up);
        }
    }
}
