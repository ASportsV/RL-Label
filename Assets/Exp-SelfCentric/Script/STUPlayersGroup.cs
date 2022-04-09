using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class STUPlayersGroup : PlayerGroup
{
    protected override string sceneName => "stu";
    protected override string dataFileName => "student_full.csv";

    protected override void LoadTracks()
    {
        testingTrack = new Queue<int>(new[] { 4, 8, 16, 25, 12, 10 });
        var rnd = new System.Random();
        trainingTrack = new Queue<int>(Enumerable.Range(0, scenes.Count)
            .Where(i => !testingTrack.Contains(i))
            .OrderBy(item => rnd.Next())
            .ToList());
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        if (time < timeStep) return;

        time -= timeStep;
        currentStep += 1;

        var players = scenes[currentScene];
        foreach(var student in players)
        {
            //var student = students[i];
            if (currentStep == student.startStep)
            {
                CreatePlayerLabelFromPos(student, true);
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
                labelAgent.SyncReset();
                go.SetActive(false);
                foreach (Transform child in go.transform)
                    child.gameObject.SetActive(false);
            }
        }

        if (currentStep >= totalStep)
        {
            if (m_RVOSettings.evaluate)
            {
                SaveMetricToJson(sceneName, totalStep, players);
            }

            LoadTrack(getNextTrack());
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
