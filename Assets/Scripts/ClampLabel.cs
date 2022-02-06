using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClampLabel : MonoBehaviour
{
    public TextMeshProUGUI namePosition;
    public GameObject labelButton;
    public Canvas player_canvas;
    Camera cam;
    public Animator thisAnim;
    public bool hasBall = false;
    void Start()
    {
        cam = Camera.main;
        thisAnim = GetComponent<Animator>();
    }

    void Update()
    {
        thisAnim.SetBool("hasBall", hasBall);
        
        //Vector3 playerPos = gameObject.transform.position;
        //Vector3 placement = cam.WorldToScreenPoint(playerPos);
        if (labelButton)
        {
            //player_canvas.transform.position = placement;
            //namePosition.transform.position = new Vector3(playerPos.x, playerPos.y + 50, playerPos.z);
        }

    }
}
