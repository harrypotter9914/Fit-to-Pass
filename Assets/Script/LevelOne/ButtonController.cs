using UnityEngine;
public class ButtonController : MonoBehaviour
{
    public Transform buttonTransform;
    public int activationWeightIndex = 12;
    public PlatformController platformController;
    public Animator buttonAnimator;
    public System.Func<bool> PowerReady;
    public Component PowerSource;
    public string PowerHint = "Restore optical power first.";
    private bool activated;
    private bool powerRejected;
    PlayerController occupant;
    void FixedUpdate()
    {
        if(GameFlow.InputBlocked)return;
        if(occupant)SetPressed(occupant.Weight>=activationWeightIndex && occupant.transform.position.y>transform.position.y);
    }
    void OnCollisionEnter2D(Collision2D c) { OnCollisionStay2D(c); }
    void OnCollisionStay2D(Collision2D c)
    {
        var p = c.gameObject.GetComponent<PlayerController>();
        if (p)
        {
            occupant=p;
            SetPressed(p.Weight >= activationWeightIndex && p.transform.position.y > transform.position.y);
        }
    }
    void OnCollisionExit2D(Collision2D c)
    {
        if (c.gameObject.GetComponent<PlayerController>())
        {
            occupant=null;
            SetPressed(false);
        }
    }
    void SetPressed(bool pressed)
    {
        if(pressed && PowerReady!=null && !PowerReady())
        {
            if(!powerRejected){GameFlow.Instance?.Toast(PowerHint);GameAudio.At("plate_release",transform.position,.35f);GetComponent<MechanismVisual>()?.Pulse();}
            powerRejected=true;return;
        }
        powerRejected=false;
        if (activated == pressed)
            return;
        activated = pressed;
        GameAudio.At(pressed ? "plate_press" : "plate_release", transform.position, .5f);
        if (platformController)
        {
            if (pressed)
                platformController.ActivatePlatform();
            else
                platformController.DeactivatePlatform();
        }
        if (buttonAnimator && buttonAnimator.enabled)
            buttonAnimator.SetBool("Ispressed", pressed);
        if (pressed)
        {
            GetComponent<MechanismVisual>()?.Pulse();

        }
    }
}
