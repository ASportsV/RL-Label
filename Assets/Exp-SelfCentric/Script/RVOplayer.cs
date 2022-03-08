using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid { get; private set; }
    [HideInInspector] public string root;
    public int currentStep = 0;

    public Transform player;
    
    public Vector3[] positions;
    public Vector3[] velocities;

    public void Init(int sId)
    {
        this.sid = sId;
        player = transform.Find("player");

        var text = transform.Find(string.Format("{0}/BackCanvas/Text", root))
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text = transform.GetSiblingIndex().ToString(); //sid.ToString();
        
        text = transform.Find(string.Format("{0}/TopCanvas/Text", root))
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text = transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/FrontCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/LeftCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/RightCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
    }

    public Vector3 velocity => velocities[currentStep];

    public void step(int idx)
    {
        if(idx < positions.Count())
        {
            currentStep = idx;
            transform.localPosition = positions[idx];
            player.transform.forward = velocities[idx].normalized;
        }
    }
}