using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

public class LabelInteraction : MonoBehaviour
{
    public Text buttonText;
    private string name;
    List<GameObject> gameObjects = new List<GameObject>();

    void Start()
    {
        name = gameObject.name;
        GetComponent<Button>().onClick.AddListener(() => LabelOnClick());
        var trigger = gameObject.AddComponent<EventTrigger>();
        var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        e.callback.AddListener(Hover);
        trigger.triggers.Add(e);
        buttonText = GameObject.Find("/Canvas/Text").GetComponent<Text>();
    }
    void LabelOnClick()
    {
        buttonText.text = name;
        Debug.Log("You have clicked the button " + name + "  /PlayerGroup/" + name + "/player-canvas-" + name);
        GameObject target = GameObject.Find("/PlayerGroup/" + name + "/player-canvas-" + name);

        target.SetActive(!target.activeSelf);
    }
    private void Hover(BaseEventData arg0)
    {
        //buttonText.text = name;
        Debug.Log("You have clicked the button " + name + "  /PlayerGroup/" + name + "/player-canvas-" + name);
        GameObject target = GameObject.Find("/PlayerGroup/" + name + "/player-canvas-" + name);
        HidePanels();
        target.SetActive(true);
    }
    private void HidePanels()
    {
        ButtonInteraction bi = gameObject.transform.parent.GetComponent<ButtonInteraction>();
        bi.LabelOnClick();
    }
}
