using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    // control
    Transform btn;
    Transform panel;
    TMPro.TMP_Dropdown dropdown;

    Transform playergroup;

    bool init = false;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        btn = transform.Find("Next");
        panel = transform.Find("Panel");

        playergroup = transform.parent.Find("PlayerGroup");
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start at UI");
        // UIs
        var trackSelect = transform.Find("TrackSelect");
        dropdown = trackSelect.GetComponent<TMPro.TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (var testId in m_RVOSettings.testingTrack)
        {
            dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("track_" + testId));
        }
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(playergroup.GetComponent<RVOPlayerGroup>().currentTrack.ToString()));
            
        // button
        btn.GetComponent<Button>().onClick.AddListener(finishTrack);
        // deactivate
        playergroup.gameObject.SetActive(false);

        m_RVOSettings.currentTaskIdx = 0;
        LoadTaskQuestion();
    }

    void LoadTaskQuestion()
    {
        var task = m_RVOSettings.tasks[m_RVOSettings.currentTaskIdx];
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(task.trackIdx.ToString()));

        // update question
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = task.task;
    }

    void finishTrack()
    {

        if (!m_RVOSettings.trackStarted && !m_RVOSettings.trackFinished)
        {
            m_RVOSettings.trackStarted = true;
            playergroup.gameObject.SetActive(true);
            panel.gameObject.SetActive(false);
            
            // btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Answer";

            //
            var task = m_RVOSettings.tasks[m_RVOSettings.currentTaskIdx];
            var groupControl = playergroup.GetComponent<RVOPlayerGroup>();
            groupControl.LoadTrack(task.trackIdx);
            // start to count the time

        } 
        else if (m_RVOSettings.trackStarted && !m_RVOSettings.trackFinished)
        {
            m_RVOSettings.trackFinished = true;
            playergroup.gameObject.SetActive(false);
            panel.gameObject.SetActive(true);

            // btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Next";
            
            // clean the scene
            text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "Your answer is ____ ";

        }
        else if (m_RVOSettings.trackStarted && m_RVOSettings.trackFinished)
        {
            // should load the next scene
            m_RVOSettings.trackStarted = false;
            m_RVOSettings.trackFinished = false;
            
            panel.gameObject.SetActive(true);
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Start";

            // load the next track
            if(m_RVOSettings.currentTaskIdx < m_RVOSettings.tasks.Count - 1)
            {
                m_RVOSettings.currentTaskIdx += 1;
                LoadTaskQuestion();
            }
            else
            {
                // update question
                text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
                text.text = "Finished! Thanks!";
                btn.GetComponent<Button>().interactable = false;
            }

            playergroup.gameObject.SetActive(false);
        }
    }
}
