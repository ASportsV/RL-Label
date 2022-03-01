using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingCamera : MonoBehaviour
{
    public Transform target;
    Rigidbody m_Rbody;
    RVOSettings m_RVOSettings;
    Vector3 destination;
    int reachedTime = 0;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }


    // Start is called before the first frame update
    void Start()
    {
        // init target
        GameObject go = new GameObject();
        go.name = "Camera_Hiden_Target";
        go.transform.SetParent(transform.parent, false);
        target = go.transform;
        target.localPosition = GetRandomSpawnPos();
        m_Rbody = go.AddComponent<Rigidbody>();
        m_Rbody.useGravity = false;
        UpdateDestination();
    }

    public Vector3 GetRandomSpawnPos()
    {
        var randomPosX = Random.Range(-m_RVOSettings.courtX, -m_RVOSettings.courtX * 0.5f);
        var randomPosZ = Random.Range(-m_RVOSettings.courtZ, m_RVOSettings.courtZ);
        var randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);

        return randomSpawnPos;
    }

    public Vector3 GetRandomDestinations()
    {
        var randomPosX = reachedTime % 2 == 1
            ? Random.Range(-m_RVOSettings.courtX, -m_RVOSettings.courtX * 0.5f)
            : Random.Range(m_RVOSettings.courtX * 0.5f, m_RVOSettings.courtX);

        var randomPosZ = Random.Range(-m_RVOSettings.courtZ, m_RVOSettings.courtZ);

        Vector3 destination = new Vector3(randomPosX, 0.5f, randomPosZ);
        return destination;
    }

    private void UpdateDestination()
    {
        destination = this.GetRandomDestinations();
        target.forward = (destination - target.localPosition).normalized;

        float[] speedRatio = { 1f, 0.8f, 0.5f, 0.3f, 0.1f };
        m_Rbody.velocity = target.forward * speedRatio[Random.Range(0, speedRatio.Length)] * 5f;
    }


    // Update is called once per frame
    private void FixedUpdate()
    {

        //// if reach the destination, move to the next
        if (Vector3.Distance(target.localPosition, destination) < 0.1)
        {
            ++reachedTime;
            // directly update
            UpdateDestination();
        }
        transform.LookAt(target);
    }
}