using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Unity.MLAgents;

public class STUPlayersGroup : PlayerGroup
{

    protected override string sceneName => "stu";
    protected override string dataFileName => "clean_stu.csv";

    protected override void LoadParameters()
    {
        m_RVOSettings.testingTrack = new Queue<int>(new[] { 4, 8, 16, 25, 12, 10 });
        string fileName = Path.Combine(Application.streamingAssetsPath, "STU_tasks.json");
        string json;
#if UNITY_EDITOR || !UNITY_ANDROID
        StreamReader r = new StreamReader(fileName);
        json = r.ReadToEnd();
#else
        WWW reader = new WWW (fileName);
        while (!reader.isDone) {}
        json = reader.text;  
#endif
        m_RVOSettings.tasks = JsonUtility.FromJson<TaskList>(json).tasks;

        var rnd = new System.Random();
        trainingTrack = new Queue<int>(Enumerable.Range(0, scenes.Count)
            .Where(i => !m_RVOSettings.testingTrack.Contains(i))
            .OrderBy(item => rnd.Next())
            .ToList());

        m_RVOSettings.xzDistThres = Academy.Instance.EnvironmentParameters.GetWithDefault("xzDistThres", 0.8f);
        m_RVOSettings.moveUnit = Academy.Instance.EnvironmentParameters.GetWithDefault("moveUnit", 1f);
        m_RVOSettings.moveSmooth = Academy.Instance.EnvironmentParameters.GetWithDefault("moveSmooth", 0.005f);
        m_RVOSettings.maxLabelSpeed = Academy.Instance.EnvironmentParameters.GetWithDefault("maxLabelSpeed", 4f);
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        if (time < timeStep) return;

        time -= timeStep;
        currentStep += 1;

        var players = scenes[currentScene];
        foreach (var student in players)
        {
            if (currentStep == student.startStep)
            {
                var playerLab  = CreatePlayerLabelFromPos(student, agentSet.Count() < numOfAgent);
                if(useBaseline) b.AddLabel(playerLab.Item2, playerLab.Item3, playerLab.Item1);
            }
            else if (currentStep > student.startStep && currentStep < (student.startStep + student.totalStep))
            {
                m_playerMap[student.id].step(currentStep - student.startStep);
            }
            else if (currentStep >= (student.startStep + student.totalStep) && m_playerMap[student.id].gameObject.activeSelf)
            {
                // deactivate
                var go = m_playerMap[student.id].gameObject;
                var labelAgent = go.GetComponentInChildren<RVOLabelAgent>();
                if (labelAgent) labelAgent?.SyncReset();

                go.SetActive(false);
                foreach (Transform child in go.transform)
                    child.gameObject.SetActive(false);

                agentSet.Remove(student.id);
            }
        }

        if (currentStep >= totalStep)
        {
            TrackFinished();
        }
    }

    protected override (int, int, int, float, float) parseRecord(string record)
    {
        string[] array = record.Split(',');
        int trackIdx = int.Parse(array[0]);
        int currentStep = int.Parse(array[1]);
        int playerIdx = int.Parse(array[2]);
        float px = float.Parse(array[3]);
        float py = float.Parse(array[4]);

        return (trackIdx, currentStep, playerIdx, px, py);
    }

}
