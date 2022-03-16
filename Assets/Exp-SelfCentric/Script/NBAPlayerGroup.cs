using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public struct Metrics
{
    public int trackId;
    public List<string> occludedObjPerStep; // sid
    public List<string> intersectedObjPerStep;
    public List<string> labelPositions;
    public List<string> labelDistToTarget;

}

public class NBAPlayerGroup : PlayerGroup
{
    // Dictionary<int, Metrics> metricsPerTrack = new Dictionary<int, Metrics>();

    protected override void LoadTasks()
    {
        m_RVOSettings.testingScenes = new Queue<int>(new[] { 0, 13, 15, 16, 21, 22 });
        m_RVOSettings.tasks = new List<Task>() {
            new Task(0, "Whose label value is XXX?"),
            new Task(0, "who has the highest value in blue team?"),
            new Task(0, "In average, which team has the highest value?"),

            new Task(13, "Whose label value is XXX?"),
            new Task(13, "who has the highest value in blue team?"),
            new Task(13, "In average, which team has the highest value?"),

            //{ 15, new Task()},
            //{ 16, new Task()},
            //{ 21, new Task()},
            //{ 22, new Task()}
        };
    }

    public override void LoadScene(int sceneIdx)
    {
        Clean();
        Init();

        currentScene = sceneIdx;
        currentStep = 0;
        var students = scenes[currentScene];
        int numPlayers = students.Count;
        
        List<GameObject> labelGroups = new List<GameObject>(),
            labels = new List<GameObject>();

        // should spawn
        for (int i = 0; i < numPlayers; ++i)
        {
            var playerLab = CreatePlayerLabelFromPos(students[i]);
            labelGroups.Add(playerLab.Item1);
            labels.Add(playerLab.Item2);
        }

        if (useBaseline)
        {
            b.InitFrom(labelGroups, labels);
        }

    }

    private void FixedUpdate()
    {
        if (m_RVOSettings.sceneFinished || !m_RVOSettings.sceneStarted) return;

        time += Time.fixedDeltaTime;
        if(time < timeStep) return;
        // update step
        time -= timeStep;
        currentStep += 1;

        var players = scenes[currentScene];
        int totalStep = players.Max(s => s.startStep + s.totalStep);
        if (currentStep < totalStep)
        {
            foreach (var player in m_playerMap.Values)
            {
                player.step(currentStep);
                if (useBaseline)
                    player.GetComponentInChildren<ComputeMetrics>().UpdateMetrics();
            }
        }
        else
        {
            if(m_RVOSettings.evaluate)
            {
                // save metrics
                SaveMetricToJson("stu", totalStep, players, useBaseline);
            }

            // replay the scene
            LoadScene(currentScene);
        }
    }

    protected override void LoadDataset()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, "nba_full_split.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();
        string[] records = pos_data.Split('\n');


        List<Dictionary<int, List<Vector3>>> tracks = new List<Dictionary<int, List<Vector3>>>();

        for (int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int trackIdx = int.Parse(array[0]);
            int playerIdx = int.Parse(array[1]);
            float px = float.Parse(array[2]);
            float py = float.Parse(array[3]);

            if ((trackIdx + 1) > tracks.Count) tracks.Add(new Dictionary<int, List<Vector3>>());
            var track = tracks[trackIdx];

            if (!track.ContainsKey(playerIdx))
            {
                track[playerIdx] = new List<Vector3>();
            }
            var playerPos = track[playerIdx];
            playerPos.Add(new Vector3(px, 0.5f, py));
        }

        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        for (int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            List<Student> track = new List<Student>();
            foreach (var entry in tracks[tIdx])
            {
                int playerIdx = entry.Key;
                Vector3[] pos = entry.Value.ToArray();
                Vector3[] vel = new Vector3[pos.Length];
                for (int i = 0; i < pos.Length - 1; ++i)
                {
                    Vector3 cur = pos[i];
                    Vector3 next = pos[i + 1];
                    vel[i] = (next - cur) / timeStep;

                    maxVel = new Vector3(Mathf.Max(vel[i].x, maxVel.x), 0, Mathf.Max(vel[i].z, maxVel.z));
                    minVel = new Vector3(Mathf.Min(vel[i].x, minVel.x), 0, Mathf.Min(vel[i].z, minVel.z));
                }
                if (pos.Length > 1) vel[pos.Length - 1] = vel[pos.Length - 2];

                Student student = new Student();
                student.id = playerIdx;
                student.positions = pos;
                student.velocities = vel;
                student.startStep = 0;
                student.totalStep = pos.Length;
                track.Add(student);
            }
            this.scenes.Add(track);
        }

        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;
    }

    protected override void Clean()
    {
        base.Clean();
        // metricsPerTrack.Clear();
    }

}
