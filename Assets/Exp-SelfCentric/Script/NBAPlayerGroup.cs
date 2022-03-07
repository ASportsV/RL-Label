using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
    Dictionary<int, Metrics> metricsPerTrack = new Dictionary<int, Metrics>();

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

    public void LoadScene(int sceneIdx)
    {
        Clean();
        Init();

        currentScene = sceneIdx;
        currentStep = 0;
        var students = scenes[currentScene];
        int numPlayers = students.Count;
        int curNumPlayers = m_playerMap.Count;
        List<GameObject> labelGroups = new List<GameObject>(),
            labels = new List<GameObject>();

        if (numPlayers > curNumPlayers)
        {
            // should spawn
            for (int i = curNumPlayers; i < numPlayers; ++i)
            {
                var playerLab = CreatePlayerLabelFromPos(students[i]);
                labelGroups.Add(playerLab.Item1);
                labels.Add(playerLab.Item2);
            }
        }

        for (int i = 0, len = m_playerMap.Count; i < len; ++i)
        {
            var player = m_playerMap[i];
            var student = students[i];
            player.positions = student.positions;
            player.velocities = student.velocities;

            if(!useBaseline)
            {
                player.GetComponentInChildren<RVOLabelAgent>().cleanMetrics();
            }
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

        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }

        var students = scenes[currentScene];
        int totalStep = students.Max(s => s.startStep + s.totalStep);
        if (currentStep < totalStep)
        {
            foreach (var player in m_playerMap.Values) player.step(currentStep);
        }
        else
        {
            if (!useBaseline && m_RVOSettings.evaluate)
            {
                // should calculate the metrix, including occlution rate, intersection rate, distance to the target, moving distance relative to the target
                var labelAgents = m_playerMap.Select(p => p.Value.GetComponentInChildren<RVOLabelAgent>());

                // collect the intersection, occlusions over time
                List<HashSet<string>> accumulatedOcclusion = new List<HashSet<string>>();
                List<HashSet<string>> accumulatedIntersection = new List<HashSet<string>>();

                for (int i = 0; i < totalStep; ++i)
                {
                    var occluded = new HashSet<string>();
                    var intersected = new HashSet<string>();
                    foreach (var labelAgent in labelAgents)
                    {
                        occluded.UnionWith(labelAgent.occludedObjectOverTime[i]);
                        intersected.UnionWith(labelAgent.intersectionsOverTime[i]);
                    }
                    accumulatedOcclusion.Add(occluded);
                    accumulatedIntersection.Add(intersected);
                }

                List<string> labelPositions = new List<string>();
                List<string> labelDistToTarget = new List<string>();
                foreach (var labelAgent in labelAgents)
                {
                    labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y));
                    labelDistToTarget.AddRange(labelAgent.distToTargetOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d));
                }

                // collect
                Metrics met = new Metrics();
                met.trackId = currentScene;
                met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
                met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
                met.labelPositions = labelPositions;
                met.labelDistToTarget = labelDistToTarget;
                metricsPerTrack[currentScene] = met;

                // save 
                using (StreamWriter writer = new StreamWriter("nba_track" + currentScene + "_met.json", false))
                {
                    writer.Write(JsonUtility.ToJson(met));
                    writer.Close();
                }
            }

            // reload the scene
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
        b.CleanEverything();
        for (int i = 0; i < m_playerMap.Count; i++)
        {
            Destroy(m_playerMap[i].gameObject);
        }

        base.Clean();
        metricsPerTrack = null;
    }

}
