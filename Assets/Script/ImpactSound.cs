using UnityEngine;
public class ImpactSound : MonoBehaviour
{
    float next;
    void OnCollisionEnter2D(Collision2D c)
    {
        if(Time.time<next || c.relativeVelocity.magnitude<1.5f)return;
        next=Time.time+.18f;
        GameAudio.At("crate_impact",transform.position,Mathf.Clamp(c.relativeVelocity.magnitude*.08f,.2f,.7f));
    }
}
