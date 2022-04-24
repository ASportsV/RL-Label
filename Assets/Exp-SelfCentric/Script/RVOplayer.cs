using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid { get; private set; }
    [HideInInspector] public string root { get; private set; }
    public int currentStep = 0;
    public Transform player;
    public Label label;
    public Vector3[] positions { get; private set; }
    public Vector3[] velocities { get; private set; }

    public bool tester = false;
    public void Init(int sId, string root, Vector3[] positions, Vector3[] velocities)
    {
        this.sid = sId;
        this.root = root;
        this.positions = positions;
        this.velocities = velocities;

        player = transform.Find("player");
        label = transform.Find("label").GetComponent<Label>();
        var text = transform.Find(string.Format("{0}/BackCanvas/Text", root))
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text =  sid.ToString(); //transform.GetSiblingIndex().ToString(); //sid.ToString();
        
        text = transform.Find(string.Format("{0}/TopCanvas/Text", root))
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString(); //transform.GetSiblingIndex().ToString();
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

    // @todo
    public Vector3 velocity => tester ? Vector3.zero : velocities[currentStep];

    public void step(int idx)
    {
        if (tester) return;
        if(idx < positions.Count())
        {
            currentStep = idx;
            transform.localPosition = positions[idx];
            player.transform.forward = velocities[idx].normalized;
            label.Collect();
        }
    }

    private void FixedUpdate()
    {
        if (!tester) return;

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        input = input.normalized;
        transform.position += input * 5f * Time.fixedDeltaTime;
    }
}