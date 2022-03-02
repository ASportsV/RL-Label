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
            
        
        btn.GetComponent<Button>().onClick.AddListener(finishTrack);

        playergroup.gameObject.SetActive(false);
    }

    void finishTrack()
    {
        if (!m_RVOSettings.trackStarted && !m_RVOSettings.trackFinished)
            // 
        {
            m_RVOSettings.trackStarted = true;
            panel.gameObject.SetActive(false);
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Answer";
            playergroup.gameObject.SetActive(true);
            // start to count the time

        } 
        else if (m_RVOSettings.trackStarted && !m_RVOSettings.trackFinished)
        {
            m_RVOSettings.trackFinished = true;
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Next";
            // clean the scene
            playergroup.gameObject.SetActive(false);
        }
        else if (m_RVOSettings.trackStarted && m_RVOSettings.trackFinished)
        {
            panel.gameObject.SetActive(true);
            // should load the next scene
            m_RVOSettings.trackStarted = false;
            m_RVOSettings.trackFinished = false;
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Start";

            // load the next track
            playergroup.GetComponent<RVOPlayerGroup>().LoadTrack();
            dropdown.value = dropdown.options
                .FindIndex(o => o.text.Contains(playergroup.GetComponent<RVOPlayerGroup>().currentTrack.ToString()));

            playergroup.gameObject.SetActive(false);
        }
    }
}
