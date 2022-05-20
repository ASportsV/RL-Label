using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;

public class NBAPlayerGroup : PlayerGroup
{
    protected override string sceneName => "nba";
    protected override string dataFileName => "nba_full_split.csv";

    protected override void LoadParameters()
    {
        m_RVOSettings.testingTrack = new Queue<int>(new[] { 0, 13, 15, 16, 21, 22 });

//        string fileName = Path.Combine(Application.streamingAssetsPath, "NBA_tasks_reorder.json");
//        string json;
//#if UNITY_EDITOR || !UNITY_ANDROID
//        StreamReader r = new StreamReader(fileName);
//        json = r.ReadToEnd();
//#else
//        WWW reader = new WWW (fileName);
//        while (!reader.isDone) {}
//        json = reader.text;  
//#endif  
//        m_RVOSettings.tasks = JsonUtility.FromJson<TaskList>(json).tasks;
        
        var rnd = new System.Random();
        trainingTrack = new Queue<int>(Enumerable.Range(0, scenes.Count)
            .Where(i => !m_RVOSettings.testingTrack.Contains(i))
            //.OrderBy(item => rnd.Next())
            .ToList());

        m_RVOSettings.xzDistThres = Academy.Instance.EnvironmentParameters.GetWithDefault("xzDistThres", 1.2f);
        m_RVOSettings.moveUnit = Academy.Instance.EnvironmentParameters.GetWithDefault("moveUnit", 3f);
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
        if (currentStep < totalStep)
        {
            foreach (var p in m_playerMap) p.Value.step(currentStep);
        }
        else
        {
            TrackFinished();
        }
    }

    protected override (int, int, int, float, float) parseRecord(string record)
    {
        string[] array = record.Split(',');
        int trackIdx = int.Parse(array[0]);
        int currentStep = 0;
        int playerIdx = int.Parse(array[1]);
        float px = float.Parse(array[2]);
        float py = float.Parse(array[3]);

        return (trackIdx, currentStep, playerIdx, px, py);
    }
}
