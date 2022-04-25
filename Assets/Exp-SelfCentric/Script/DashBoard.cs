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
            var task = m_RVOSettings.CurrentTask;
            // var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            // text.text = (m_RVOSettings.ansTime).ToString("F") + "s";
            var text = panel.Find("Q").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "Task" + m_RVOSettings.currentTaskIdx + ": " + task.Q;

            text = panel.Find("q11").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "CD";
            text.color = new Color (255,255,255, 1f);
            text = panel.Find("q12").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "EF";
            text.color = new Color (255,255,255, 1f);
            text = panel.Find("q21").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "MN";
            text.color = new Color (255,255,255, 1f);
            text = panel.Find("q22").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "XY";
            text.color = new Color (255,255,255, 1f);

            if(task.type == "identify")
            {
                text = panel.Find("q21").GetComponent<TMPro.TextMeshProUGUI>();
                text.text = "goal";
                text.color = new Color (1,0,0, 1f);
            }
            else if(task.type == "compare")
            {
                text = panel.Find("q22").GetComponent<TMPro.TextMeshProUGUI>();
                text.text = "goal";
                text.color = new Color (1,0,0, 1f);
            }
            else if(task.type == "summary")
            {
                text = panel.Find("q12").GetComponent<TMPro.TextMeshProUGUI>();
                text.text = "goal";
                text.color = new Color (1,0,0, 1f);
            }

        }
    }
}
