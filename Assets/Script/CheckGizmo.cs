using UnityEngine;



public class CheckGizmo : MonoBehaviour

{

    // 定义小圆圈的半径

    public float radius = 0.1f;



    // 在 Scene 视图中绘制 Gizmo

    void OnDrawGizmos()

    {

        Gizmos.color = Color.green; // 设置 Gizmo 的颜色

        Gizmos.DrawSphere(transform.position, radius); // 在 GroundCheck 位置绘制一个小圆圈

    }

}
