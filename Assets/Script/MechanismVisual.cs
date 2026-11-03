using UnityEngine;
public class MechanismVisual : MonoBehaviour
{
    public Transform art;
    Vector3 scale;
    float bottom;
    SpriteRenderer sprite;
    float pulse;
    void Start()
    {
        if (art)
        {
            scale = art.localScale;
            sprite = art.GetComponent<SpriteRenderer>();
            bottom = sprite.bounds.min.y;
        }
    }
    public void Pulse()
    {
        pulse = 1;
    }
    void Update()
    {
        if (!art)
            return;
        pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 4);
        art.localScale = Vector3.Scale(scale, new Vector3(1 + pulse * .13f, 1 - pulse * .2f, 1));
        art.position += Vector3.up * (bottom - sprite.bounds.min.y);
    }
}
