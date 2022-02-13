using System.Collections;
using System.Collections.Generic;
using UnityEngine;



using Unity.Mathematics;

sealed class OneEuroFilter2
{
    #region Public properties

    public float Beta { get; set; }
    public float MinCutoff { get; set; }

    #endregion

    #region Public step function

    public float Step(float t, float x)
    {
        var t_e = t - _prev.t;

        // Do nothing if the time difference is too small.
        if (t_e < 1e-5f) return _prev.x;

        var dx = (x - _prev.x) / t_e;
        var dx_res = math.lerp(_prev.dx, dx, Alpha(t_e, DCutOff));

        var cutoff = MinCutoff + Beta * math.length(dx_res);
        var x_res = math.lerp(_prev.x, x, Alpha(t_e, cutoff));

        _prev = (t, x_res, dx_res);

        return x_res;
    }

    #endregion

    #region Private class members

    const float DCutOff = 1.0f;

    static float Alpha(float t_e, float cutoff)
    {
        var r = 2 * math.PI * cutoff * t_e;
        return r / (r + 1);
    }

    #endregion

    #region Previous state variables as a tuple

    (float t, float x, float dx) _prev;

    #endregion
}

public class MovingAverage
{
    private Queue<float> samples = new Queue<float>();
    private int windowSize = 16;
    private float sampleAccumulator;
    public float Average { get; private set; }

    /// <summary>
    /// Computes a new windowed average each time a new sample arrives
    /// </summary>
    /// <param name="newSample"></param>
    public void ComputeAverage(float newSample)
    {
        sampleAccumulator += newSample;
        samples.Enqueue(newSample);

        if (samples.Count > windowSize)
        {
            sampleAccumulator -= samples.Dequeue();
        }

        Average = sampleAccumulator / samples.Count;
    }
}


public class CamStablizer : MonoBehaviour
{
    OneEuroFilter2 _filter = new OneEuroFilter2();
    OneEuroFilter<Quaternion> rotationFilter;
    public bool filterOn = true;
    public float filterFrequency = 1f;
    public float filterMinCutoff = 0.9f;
    public float filterBeta = 0.0005f;
    public float filterDcutoff = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        rotationFilter = new OneEuroFilter<Quaternion>(filterFrequency);
        _filter.Beta = 0.0005f;
        _filter.MinCutoff = 0.9f;
    }


    private void FixedUpdate()
    {

        Vector3 euler = transform.rotation.eulerAngles;
        //transform.rotation = Quaternion.Euler(euler.x, _filter.Step(Time.time, euler.y), euler.z);
        rotationFilter.UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
        transform.rotation = rotationFilter.Filter(transform.parent.rotation);

        print("Diff: " + (transform.parent.eulerAngles - transform.eulerAngles).ToString());
        //Quaternion.Slerp(preRot, transform.rotation, 0.5f);
        //preRot = transform.rotation;
    }
}
