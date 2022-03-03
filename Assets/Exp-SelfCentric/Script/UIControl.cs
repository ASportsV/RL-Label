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
        LoadTaskQuestionInUI(m_RVOSettings.currentTaskIdx);
    }

    void LoadTaskQuestionInUI(int taskIdx)
    {
        var task = m_RVOSettings.tasks[taskIdx];
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(task.sceneIdx.ToString()));

        // update question
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = task.task;
        
    }

    void ClickButton()
    {
        bool beforeTrial = !m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool inTrial = m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool afterTrial = m_RVOSettings.sceneStarted && m_RVOSettings.sceneFinished;

        if (beforeTrial)
        {
            m_RVOSettings.sceneStarted = true;
            // show the scene
            playergroup.gameObject.SetActive(true);
            // hide the panel
            panel.gameObject.SetActive(false);
            
            // update the text in the btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Answer";

            // load and start the scene
            var task = m_RVOSettings.tasks[m_RVOSettings.currentTaskIdx];
            var groupControl = playergroup.GetComponent<NBAPlayerGroup>();
            if(groupControl)
            {
                groupControl.LoadScene(task.sceneIdx);

            }
            else
            {
                playergroup.GetComponent<STUPlayersGroup>().LoadScene(task.sceneIdx);
            }

            // start to count the time
        } 
        else if (inTrial)
        {
            m_RVOSettings.sceneFinished = true;
            // hide the scene
            playergroup.gameObject.SetActive(false);
            // show the panel
            panel.gameObject.SetActive(true);

            // update the text in the btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Next";
            
            // update the text in the panel
            text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "Your answer is ____ ";

        }
        else if (afterTrial)
        {
            m_RVOSettings.sceneStarted = false;
            m_RVOSettings.sceneFinished = false;
            
            // show the panel
            panel.gameObject.SetActive(true);
            // hide the scene
            playergroup.gameObject.SetActive(false);

            // update the text in the btn
            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            text.text = "Start";

            // load the next task, only update the UI question, but not load and start the scene
            if(m_RVOSettings.currentTaskIdx < m_RVOSettings.tasks.Count - 1)
            {
                m_RVOSettings.currentTaskIdx += 1;
                LoadTaskQuestionInUI(m_RVOSettings.currentTaskIdx);
            }
            else
            {
                // finish
                text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
                text.text = "Finished! Thanks!";
                btn.GetComponent<Button>().interactable = false;
            }
        }
    }
}
