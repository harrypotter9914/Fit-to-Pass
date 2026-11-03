using UnityEngine;
public class SpringController : MonoBehaviour
{
    public Animator springAnimator;
    public float weakForce = 5f, mediumForce = 10f, strongForce = 15f;
    private float lastBounce = -10;
    void OnCollisionEnter2D(Collision2D c)
    {
        var p = c.gameObject.GetComponent<PlayerController>();
        if (!p || p.transform.position.y < transform.position.y || Time.time - lastBounce < .15f)
            return;
        lastBounce = Time.time;
        if (springAnimator && springAnimator.enabled)
            springAnimator.SetInteger("SpringState", p.Size);
        p.Launch(p.Size == 0 ? strongForce : p.Size == 2 ? weakForce : mediumForce);
        GetComponent<MechanismVisual>()?.Pulse();
        GameAudio.At("spring_" + p.Size, transform.position, .6f);
    }
    void OnCollisionExit2D(Collision2D c)
    {
        if (!c.gameObject.GetComponent<PlayerController>()) return;
        GameAudio.At("spring_return", transform.position, .3f);
        if (springAnimator && springAnimator.enabled)
            springAnimator.SetInteger("SpringState", 3);
    }
}
