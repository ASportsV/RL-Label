using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCar : MonoBehaviour
{

    public bool myCar = false;

    // Start is called before the first frame update
    void Start()
    {
        //this.transform.Find("Camera").gameObject.SetActive(myCar);
        this.transform.Find("ARLabel").gameObject.SetActive(!myCar);
    }

}
