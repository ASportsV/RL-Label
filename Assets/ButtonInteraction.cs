using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonInteraction : MonoBehaviour
{
    public bool isUpdated = false;
    List<GameObject> gameObjects = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        //GetComponent<Button>().onClick.AddListener(() => LabelOnClick());
        //gameObject.GetComponentInChildren<Text>().text = "Clear";
    }

    // Update is called once per frame
    void Update()
    {
        if (!isUpdated)
        {
           gameObjects.AddRange(gameObject.transform.GetComponentsInChildren<Button>().Select(x => x.gameObject));
            print("buttoninteraction count: " + gameObjects.Count);
            if (gameObjects.Count > 10) isUpdated = true;
        }
       
    }
    public void LabelOnClick()
    {
        foreach (var item in gameObjects)
        {
            if( item.name != "Button")
            {
                GameObject target = GameObject.Find("/PlayerGroup/" + item.name + "/player-canvas-" + item.name);
                target.SetActive(false);
            }
        }
    }
}
