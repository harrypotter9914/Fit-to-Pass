using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    
    [SerializeField] Camera m_Camera;
    [SerializeField] float m_SmoothTime = 0.5f;
    Vector2 _currentSpeed;
    void Start()
    {
        m_Camera = Camera.main;
    }
    private void FixedUpdate()
    {
        var tmp = Vector2.SmoothDamp(m_Camera.transform.position, transform.position, ref _currentSpeed, m_SmoothTime);
        m_Camera.transform.position = new Vector3(tmp.x, tmp.y, m_Camera.transform.position.z);
    }

}
