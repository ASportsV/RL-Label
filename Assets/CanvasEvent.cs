using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class CanvasEvent : MonoBehaviour
{
    private XRController controller1;
    private XRController controller2;
    private XRRayInteractor interactor1;
    private XRRayInteractor interactor2;
    public Text canvasText;
    private bool isUpdated = false;

    private void Update()
    {
        if (!isUpdated)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            gameObjects.AddRange(gameObject.GetComponentsInChildren<Button>().Select(x => x.gameObject));
            gameObjects.AddRange(gameObject.GetComponentsInChildren<Slider>().Select(x => x.gameObject));
            gameObjects.AddRange(gameObject.GetComponentsInChildren<Dropdown>().Select(x => x.gameObject));

            foreach (var item in gameObjects)
            {
                var trigger = item.AddComponent<EventTrigger>();
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                e.callback.AddListener(Hover);
                trigger.triggers.Add(e);
            }
            if (gameObjects.Count >10) isUpdated = true;
        }
    }

    private void GetControllers()
    {
        if (controller1 == null || controller2 == null)
        {
            var controllers = FindObjectsOfType<XRController>();
            if (controllers.Length > 0)
            {
                controller1 = controllers[0];
                interactor1 = controller1.gameObject.GetComponent<XRRayInteractor>();
            }
            if (controllers.Length > 1)
            {
                controller2 = controllers[1];
                interactor2 = controller2.gameObject.GetComponent<XRRayInteractor>();
            }
        }
    }

    private void Hover(BaseEventData arg0)
    {
        GetControllers();

        if (interactor1.enabled)
        {
            controller1.inputDevice.SendHapticImpulse(0, interactor1.hapticHoverEnterIntensity, interactor1.hapticHoverEnterDuration);
            canvasText.text = "hovered";
        }
        else if (interactor2.enabled)
        {
            controller2.inputDevice.SendHapticImpulse(0, interactor2.hapticHoverEnterIntensity, interactor2.hapticHoverEnterDuration);
            canvasText.text = "hovered";

        }
    }
}