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
        foreach (var testId in m_RVOSettings.testingScenes)
        {
            dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("track_" + testId));
        }

        var groupControl = playergroup.GetComponent<NBAPlayerGroup>();
        int currentTrack;
        if(groupControl)
        {
            currentTrack = groupControl.currentScene;
        }
        else
        {
            currentTrack = playergroup.GetComponent<STUPlayersGroup>().currentScene;
        }
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(currentTrack.ToString()));
            
        // button
        btn.GetComponent<Button>().onClick.AddListener(ClickButton);
        // deactivate
        playergroup.gameObject.SetActive(false);

        m_RVOSettings.currentTaskIdx = 0;
        LoadTaskQuestion(m_RVOSettings.currentTaskIdx);
    }

    void LoadTaskQuestion(int taskIdx)
    {
        var task = m_RVOSettings.tasks[taskIdx];
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(task.trackIdx.ToString()));

        // need to update the technique, scene, and task

        // update question
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = task.task;
        
    }

    void ClickButton()
    {

        if (!m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished)
        {
            m_RVOSettings.sceneStarted = true;
            playergroup.gameObject.SetActive(true);
            panel.gameObject.SetActive(false);
            
            // btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Answer";

            //
            var task = m_RVOSettings.tasks[m_RVOSettings.currentTaskIdx];
            var groupControl = playergroup.GetComponent<NBAPlayerGroup>();
            if(groupControl)
            {
                groupControl.LoadScene(task.trackIdx);

            }
            else
            {
                playergroup.GetComponent<STUPlayersGroup>().LoadScene(task.trackIdx);
            }

            // start to count the time
        } 
        else if (m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished)
        {
            m_RVOSettings.sceneFinished = true;
            playergroup.gameObject.SetActive(false);
            panel.gameObject.SetActive(true);

            // btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Next";
            
            // clean the scene
            text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "Your answer is ____ ";

        }
        else if (m_RVOSettings.sceneStarted && m_RVOSettings.sceneFinished)
        {
            // should load the next scene
            m_RVOSettings.sceneStarted = false;
            m_RVOSettings.sceneFinished = false;
            
            panel.gameObject.SetActive(true);
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Start";

            // load the next track
            if(m_RVOSettings.currentTaskIdx < m_RVOSettings.tasks.Count - 1)
            {
                m_RVOSettings.currentTaskIdx += 1;
                LoadTaskQuestion(m_RVOSettings.currentTaskIdx);
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
