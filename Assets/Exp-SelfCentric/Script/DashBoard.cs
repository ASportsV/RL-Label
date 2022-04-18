using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashBoard : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    Transform panel;
    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        panel = transform.Find("Panel");
    }

    void Update()
    {
        bool beforeTrial = !m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool inTrial = m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool afterTrial = m_RVOSettings.sceneStarted && m_RVOSettings.sceneFinished;

        if(beforeTrial)
        {
            m_RVOSettings.ansTime = 0;
        }
        else if(inTrial)
        {
            m_RVOSettings.ansTime += Time.deltaTime;
            var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = (m_RVOSettings.ansTime % 60).ToString("F") + "s";
            text = panel.Find("Q").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = m_RVOSettings.CurrentTask.Q;
        }
    }
}
