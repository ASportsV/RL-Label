using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    public PrimaryButtonWatcher watcher;
    // control
    Transform panel;
    TMPro.TMP_Dropdown dropdown;

    Transform playergroup;

    PlayerGroup groupControl;


    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        panel = transform.Find("Panel");
        playergroup = transform.parent.Find("PlayerGroup");
    }

    void Start()
    {
        // button
#if UNITY_EDITOR || !UNITY_ANDROID
        transform.Find("Next").GetComponent<Button>().onClick.AddListener(MouseClickButton);
        transform.Find("Task").GetComponent<Button>().onClick.AddListener(MouseClickTaskId);
#else
        transform.Find("Next").gameObject.SetActive(false);
        transform.Find("Task").gameObject.SetActive(false);
        watcher.primaryButtonPress.AddListener(ClickButton);
        watcher.secondaryButtonPress.AddListener(IncreUserId);
        watcher.triggerButtonPress.AddListener(IncreTaskId);
#endif
        // deactivate
        // playergroup.gameObject.SetActive(false);

    }
    void MouseClickButton()
    {
        ClickButton(true);
    }

    void MouseClickTaskId()
    {
        IncreTaskId(true);
    }
    void IncreUserId(bool press)
    {
        if (!press || m_RVOSettings.setUserId) return;
        m_RVOSettings.userId = (++m_RVOSettings.userId % 19);
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = "UserId:" + m_RVOSettings.userId;
    }

    void IncreTaskId(bool press)
    {
        Debug.Log("Click next task " + press);
        if(!press || m_RVOSettings.setUserId) return;
        m_RVOSettings.NextTask(true);
        var text = panel.Find("TaskId").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = "Start from Task" + m_RVOSettings.currentTaskIdx.ToString(); 
    }

    void LoadTaskQuestionInUI(Task task)
    {
        // dropdown.value = dropdown.options
        //     .FindIndex(o => o.text.Contains(task.track_id.ToString()));
        // update question
        var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
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


    void ClickButton(bool press)
    {
        if (!press) return;
        if(!m_RVOSettings.setUserId)
        {
            m_RVOSettings.setUserId = true;
            m_RVOSettings.getOrderByUserId();
            panel.Find("TaskId").gameObject.SetActive(false);
            playergroup.gameObject.SetActive(true);
            groupControl = playergroup.GetComponent<PlayerGroup>();
            LoadTaskQuestionInUI(m_RVOSettings.CurrentTask);
            return;
        }

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

                // load and start the scene
                var task = m_RVOSettings.CurrentTask;
                groupControl.LoadTrack(task.track_id);
        }
        else if (inTrial) // -> exectue the code for the next
        {
            m_RVOSettings.sceneFinished = true;
            // hide the scene
            playergroup.gameObject.SetActive(false);
            // show the panel
            panel.gameObject.SetActive(true);

            // update the text in the panel
            var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            text.text = "Task" + m_RVOSettings.currentTaskIdx + ": Your answer is __ . Your mental load is __ (1~7)."; // You took " + m_RVOSettings.ansTime.ToString("F") + "s.";
            m_RVOSettings.saveToSheet();

        }
        else if (afterTrial) // -> exectue the code for the next
        {
            // hide the scene
            playergroup.gameObject.SetActive(false);
            // if(m_RVOSettings.shouldRate && m_RVOSettings.currentTaskIdx % 3 == 2) // should rate
            // {
            //     m_RVOSettings.shouldRate = false;
            //     // disable all text
            //     var texts = panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            //     foreach(TMPro.TextMeshProUGUI child in texts)
            //         child.gameObject.SetActive(false);
            //     panel.Find("Panel (1)").gameObject.SetActive(false);
            //     // load the ranking text
            //     var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
            //     text.gameObject.SetActive(true);
            //     text.text = "Plz order your mental load of the last three trails descendingly:";
            //     // load the videos
            //     var videos = panel.parent.Find("Videos");
            //     videos.gameObject.SetActive(true);
            //     // set the sources
            //     var rawImages = videos.GetComponentsInChildren<RawImage>();
            //     // deactivate all
            //     foreach(var rawImage in rawImages)
            //     {
            //         rawImage.gameObject.SetActive(false);
            //         // var rect = rawImage.GetComponent<RectTransform>();
            //         // rect.anchoredPosition = new Vector2(200, -38);
            //     }

            //     for(int i = 0; i < 3; ++i)
            //     {
            //         var currentTech = m_RVOSettings.techOrder[i];

            //         // var rawImage = rawImages[i];
            //         Transform target;
            //         if(currentTech == Tech.No)
            //         {
            //             target = m_RVOSettings.sceneName == "nba"
            //                 ? videos.Find("1")
            //                 : m_RVOSettings.sceneName == "stu"
            //                 ? videos.Find("4")
            //                 : m_RVOSettings.currentTaskIdx <= 8
            //                 ? videos.Find("1")
            //                 : videos.Find("4");
            //         //     rawImage.texture = 
            //         //         ? m_RVOSettings.videoTextures[0]
            //         //         : m_RVOSettings.sceneName == "stu"
            //         //         ? m_RVOSettings.videoTextures[3]
            //         //         : m_RVOSettings.currentTaskIdx <= 8
            //         //         ? m_RVOSettings.videoTextures[0]
            //         //         : m_RVOSettings.videoTextures[3];
            //         }
            //         else if (currentTech == Tech.Opti)
            //         {
            //             target = m_RVOSettings.sceneName == "nba"
            //                 ? videos.Find("2")
            //                 : m_RVOSettings.sceneName == "stu"
            //                 ? videos.Find("5")
            //                 : m_RVOSettings.currentTaskIdx <= 8
            //                 ? videos.Find("2")
            //                 : videos.Find("5");
            //             //     rawImage.texture = m_RVOSettings.sceneName == "nba"
            //             //         ? m_RVOSettings.videoTextures[1]
            //             //         : m_RVOSettings.sceneName == "stu"
            //             //         ? m_RVOSettings.videoTextures[4]
            //             //         : m_RVOSettings.currentTaskIdx <= 8
            //             //         ? m_RVOSettings.videoTextures[1]
            //             //         : m_RVOSettings.videoTextures[4];
            //         }
            //         else //(currentTech == Tech.Ours)
            //         {
            //             target = m_RVOSettings.sceneName == "nba"
            //                 ? videos.Find("3")
            //                 : m_RVOSettings.sceneName == "stu"
            //                 ? videos.Find("6")
            //                 : m_RVOSettings.currentTaskIdx <= 8
            //                 ? videos.Find("3")
            //                 : videos.Find("6");
            //             //     rawImage.texture = m_RVOSettings.sceneName == "nba"
            //             //         ? m_RVOSettings.videoTextures[2]
            //             //         : m_RVOSettings.sceneName == "stu"
            //             //         ? m_RVOSettings.videoTextures[5]
            //             //         : m_RVOSettings.currentTaskIdx <= 8
            //             //         ? m_RVOSettings.videoTextures[2]
            //             //         : m_RVOSettings.videoTextures[5];
            //         }
            //         if (target == null) return;

            //         target.gameObject.SetActive(true);
            //         var rect = target.GetComponent<RectTransform>();
            //         if (i == 0) rect.anchoredPosition = new Vector2(-40f, -38f);
            //         else if(i == 1) rect.anchoredPosition = new Vector2(0f, -38f);
            //         else if(i == 2) rect.anchoredPosition = new Vector2(40f, -38f);
            //     }
            // }
            // else 
            // {
                // activate all texts
                foreach(Transform child in panel)
                {
                    if(child.name == "TaskId") continue;
                    child.gameObject.SetActive(true);
                }
                // deactivate the videos
                panel.parent.Find("Videos").gameObject.SetActive(false);
                //
                m_RVOSettings.sceneStarted = false;
                m_RVOSettings.sceneFinished = false;
                m_RVOSettings.shouldRate = true;
                // show the panel
                panel.gameObject.SetActive(true);
                // load the next task, only update the UI question, but not load and start the scene
                if(m_RVOSettings.currentTaskIdx != -1)
                    m_RVOSettings.NextTask();

                if (m_RVOSettings.currentTaskIdx != -1)
                {
                    LoadTaskQuestionInUI(m_RVOSettings.CurrentTask);
                }
                else
                {
                    // finish
                    var text = panel.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
                    text.text = "Finished! Thanks!";
                    m_RVOSettings.sceneStarted = true;
                    m_RVOSettings.sceneFinished = true;
                }
            // }
        }
    }
}
