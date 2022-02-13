using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMyCar : MonoBehaviour
{
    public GameObject following;

    OneEuroFilter<Quaternion> rotationFilter;
    public bool filterOn = true;
    public float filterFrequency = 2f;
    public float filterMinCutoff = 0.09f;
    public float filterBeta = 0.0005f;
    public float filterDcutoff = 1.0f;

    void Start()
    {
        rotationFilter = new OneEuroFilter<Quaternion>(filterFrequency);
    }

    void Update()
    {
        transform.localPosition = following.transform.localPosition + new Vector3(0.08160067f, 0.5811662f, 0f);

        rotationFilter.UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);

        transform.rotation = rotationFilter.Filter(following.transform.rotation);
    }

    private void FixedUpdate()
    {

        
    }

}
