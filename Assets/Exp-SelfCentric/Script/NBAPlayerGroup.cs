using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class NBAPlayerGroup : PlayerGroup
{
    protected override string sceneName => "nba";
    protected override string dataFileName => "nba_full_split.csv";

    protected override void LoadTracks()
    {
        testingTrack = new Queue<int>(new[] { 0, 13, 15, 16, 21, 22 });
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
