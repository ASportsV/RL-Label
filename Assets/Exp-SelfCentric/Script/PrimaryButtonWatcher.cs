using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

[System.Serializable]
public class PrimaryButtonEvent : UnityEvent<bool> { }
public class SecondaryButtonEvent : UnityEvent<bool> { }

public class PrimaryButtonWatcher : MonoBehaviour
{
    public PrimaryButtonEvent primaryButtonPress;
    public SecondaryButtonEvent secondaryButtonPress;

    private bool lastButtonState = false;
    private bool lastSecondaryButtonState = false;
    private List<InputDevice> devicesWithPrimaryButton;

    private void Awake()
    {
        if (primaryButtonPress == null)
        {
            primaryButtonPress = new PrimaryButtonEvent();
        }
        if (secondaryButtonPress == null)
        {
            secondaryButtonPress = new SecondaryButtonEvent();
        }

        devicesWithPrimaryButton = new List<InputDevice>();
    }

    void OnEnable()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach (InputDevice device in allDevices)
            InputDevices_deviceConnected(device);

        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
        devicesWithPrimaryButton.Clear();
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
        {
            devicesWithPrimaryButton.Add(device); // Add any devices that have a primary button.
        }
    }

    private void InputDevices_deviceDisconnected(InputDevice device)
    {
        if (devicesWithPrimaryButton.Contains(device))
            devicesWithPrimaryButton.Remove(device);
    }

    void Update()
    {
        bool primaryTempState = false;
        bool secondaryTempState = false;
        foreach (var device in devicesWithPrimaryButton)
        {
            bool primaryButtonState = false;
            primaryTempState = device.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButtonState) // did get a value
                && primaryButtonState // the value we got
                || primaryTempState; // cumulative result from other controllers

            bool secondaryButtonState = false;
            secondaryTempState = device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButtonState)
                && secondaryButtonState
                || secondaryTempState;
        }

        if (secondaryTempState != lastSecondaryButtonState)
        {
            secondaryButtonPress.Invoke(secondaryTempState);
            lastSecondaryButtonState = secondaryTempState;
        }

        if (primaryTempState != lastButtonState) // Button state changed since last frame
        {
            primaryButtonPress.Invoke(primaryTempState);
            lastButtonState = primaryTempState;
        }
    }
}