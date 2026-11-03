using UnityEngine;
[DefaultExecutionOrder(210)]
public class PlayerFoley : MonoBehaviour
{
    PlayerController player;
    SoundEmitter sound;
    bool grounded, initialized;
    Vector2 previous;
    float steps;
    void Start(){player=GetComponent<PlayerController>();sound=gameObject.AddComponent<SoundEmitter>();previous=transform.position;}
    void FixedUpdate()
    {
        if(GameFlow.InputBlocked)return;
        var position=(Vector2)transform.position;
        if(initialized && !grounded && player.Grounded)sound.Cue("land_"+player.Size,.6f);
        if(player.Grounded && Mathf.Abs(player.HorizontalInput)>.1f)
        {
            float distance=Mathf.Abs(position.x-previous.x);
            if(distance<1)steps+=distance;
            if(steps>(player.Size==0?.65f:player.Size==1?.8f:1f)){steps=0;sound.Cue("step_"+player.Size,.38f);}
        }
        else steps=0;
        grounded=player.Grounded;previous=position;initialized=true;
        var hold=GetComponent<MassLock>();sound.Loop(hold && hold.Locked?"lock_hum":null,.13f);
    }
}
