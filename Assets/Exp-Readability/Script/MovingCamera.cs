using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingCamera : MonoBehaviour
{
    ARLabelSettings m_ARLabelSettings;
    public Transform target;
    Rigidbody m_Rbody;


    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
    }


    // Start is called before the first frame update
    void Start()
    {
        // init target
        GameObject go = new GameObject();
        go.name = "Camera_Hiden_Target";
        go.transform.SetParent(transform.parent, false);
        target = go.transform;
        m_Rbody = go.AddComponent<Rigidbody>();
        m_Rbody.useGravity = false;
        go.AddComponent<Player>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        transform.LookAt(target);
    }
}
