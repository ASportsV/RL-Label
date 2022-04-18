using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    public PrimaryButtonWatcher watcher;
    // control
    Transform btn;
    Transform panel;
    TMPro.TMP_Dropdown dropdown;

    Transform playergroup;

    PlayerGroup groupControl;

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
        foreach (var testId in m_RVOSettings.testingTrack)
        {
            dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("track_" + testId));
        }

        groupControl = playergroup.GetComponent<PlayerGroup>();
        // if(groupControl)
        // {
        //     currentTrack = groupControl.currentScene;
        // }
        // else
        // {
        //     currentTrack = playergroup.GetComponent<STUPlayersGroup>().currentScene;
        // }
        int currentScene = groupControl.currentScene;
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(currentScene.ToString()));
            
        // button
        // btn.GetComponent<Button>().onClick.AddListener(ClickButton);
        watcher.primaryButtonPress.AddListener(ClickButton);
        // deactivate
        playergroup.gameObject.SetActive(false);

        LoadTaskQuestionInUI(m_RVOSettings.CurrentTask);
    }

    void LoadTaskQuestionInUI(Task task)
    {
        dropdown.value = dropdown.options
            .FindIndex(o => o.text.Contains(task.track_id.ToString()));

        // update question
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = task.Q;
    }

    void ClickButton(bool press)
    {
        if(!press) return;
        bool beforeTrial = !m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool inTrial = m_RVOSettings.sceneStarted && !m_RVOSettings.sceneFinished;
        bool afterTrial = m_RVOSettings.sceneStarted && m_RVOSettings.sceneFinished;

        if (beforeTrial) // -> exectue the code for the next
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
            var task = m_RVOSettings.CurrentTask;
            groupControl.LoadTrack(task.track_id);
            // if(groupControl)
            // {
            // }
            // else
            // {
            //     playergroup.GetComponent<STUPlayersGroup>().useBaseline =
            //         m_RVOSettings.CurrentTech == Tech.Opti;
            //     playergroup.GetComponent<STUPlayersGroup>().LoadScene(task.sceneIdx);
            // }

            // start to count the time
        } 
        else if (inTrial) // -> exectue the code for the next
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
        else if (afterTrial) // -> exectue the code for the next
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
            if(m_RVOSettings.currentTaskIdx != - 1)
            {
                m_RVOSettings.NextTask();
                LoadTaskQuestionInUI(m_RVOSettings.CurrentTask);
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
