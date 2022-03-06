using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEditor;

public struct Student
{
    public int id;
    public Vector3[] positions;
    public Vector3[] velocities;
    public int totalStep;
    public int startStep;
}

public class STUPlayersGroup : PlayerGroup
{


    protected override void LoadTasks()
    {
        // load the study data
        m_RVOSettings.testingScenes = new Queue<int>(new[] { 4, 8, 16, 25, 12, 10 });
        m_RVOSettings.tasks = new List<Task>() {
            new Task(4, "Whose label value is XXX?"),
            new Task(4, "who has the highest value in blue team?"),
            new Task(4, "In average, which team has the highest value?"),

            new Task(8, "Whose label value is XXX?"),
            new Task(8, "who has the highest value in blue team?"),
            new Task(8, "In average, which team has the highest value?"),

        };
    }

    public void LoadScene(int sceneId)
    {
        // clean the old students
        foreach (var entry in m_playerMap.Where(p => p.Value.gameObject.activeSelf))
        {
            var p = entry.Value;
            if (p.gameObject.activeSelf)
            {
                if(!useBaseline)
                {
                    p.GetComponentInChildren<RVOLabelAgent>().SyncReset();
                }
            }
            else
            {
                p.transform.GetChild(1).gameObject.SetActive(true);
            }

            if(!useBaseline)
            {
                p.GetComponentInChildren<RVOLabelAgent>().cleanMetrics();
            }
        }

        // remove all existing
        foreach (var entry in m_playerMap)
        {
            var p = entry.Value;
            Destroy(p.gameObject);
        }
        m_playerMap.Clear();

        // reset
        currentScene = sceneId;
        currentStep = 0;
        var students = scenes[currentScene];
        List<GameObject> labelGroups = new List<GameObject>(),
            labels = new List<GameObject>();

        for (int i = 0, len = students.Count; i < len; ++i)
        {
            var student = students[i];
            if (currentStep == student.startStep)
            {
                var playerLab = CreatePlayerLabelFromPos(student);
                labelGroups.Add(playerLab.Item1);
                labels.Add(playerLab.Item2);
            }
        }

        if(useBaseline)
        {
            b.InitFrom(labelGroups, labels);
        }
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;

            var students = scenes[currentScene];
            int totalStep = students.Max(s => s.startStep + s.totalStep);

            // add or move the student
            foreach(var student in students)
            {
                if (currentStep == student.startStep)
                {
                    var playerLab = CreatePlayerLabelFromPos(student);
                    if(useBaseline)
                    {
                        b.AddLabel(playerLab.Item1, playerLab.Item2);
                    }
                }
                else if (currentStep > student.startStep && currentStep < (student.startStep + student.totalStep))
                {
                    m_playerMap[student.id].step(currentStep - student.startStep);
                }
                else if (currentStep >= (student.startStep + student.totalStep) && m_playerMap[student.id].gameObject.activeSelf)
                {
                    // deactivate
                    var go = m_playerMap[student.id].gameObject;

                    if(!useBaseline)
                    {
                        var labelAgent = go.GetComponentInChildren<RVOLabelAgent>();
                        labelAgent.SyncReset();
                    }

                    go.SetActive(false);
                    foreach (Transform child in go.transform)
                        child.gameObject.SetActive(false);
                }
            }


            if (currentStep >= totalStep)
            {
                if(!useBaseline)
                {
                    // evaluation
                    if (m_RVOSettings.evaluate)
                    {
                        // collect the intersection, occlusions over time
                        List<HashSet<string>> accumulatedOcclusion = new List<HashSet<string>>();
                        List<HashSet<string>> accumulatedIntersection = new List<HashSet<string>>();
                        foreach (var entry in m_playerMap)
                        {
                            foreach (Transform child in entry.Value.transform)
                                child.gameObject.SetActive(true);
                        }

                        for (int i = 0; i < totalStep; ++i)
                        {
                            var occluded = new HashSet<string>();
                            var intersected = new HashSet<string>();

                            foreach (var student in students.Where(s => i >= s.startStep && i < (s.startStep + s.totalStep)))
                            {
                                var labelAgent = m_playerMap[student.id].gameObject.GetComponentInChildren<RVOLabelAgent>();

                                occluded.UnionWith(labelAgent.occludedObjectOverTime[i - student.startStep]);
                                intersected.UnionWith(labelAgent.intersectionsOverTime[i - student.startStep]);
                            }
                            accumulatedOcclusion.Add(occluded);
                            accumulatedIntersection.Add(intersected);
                        }

                        List<string> labelPositions = new List<string>();
                        List<string> labelDistToTarget = new List<string>();
                        foreach (var student in students)
                        {
                            var labelAgent = m_playerMap[student.id].GetComponentInChildren<RVOLabelAgent>();
                            labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y));
                            labelDistToTarget.AddRange(labelAgent.distToTargetOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d));
                            Debug.Log("Occ Step of " + student.id + " is " + labelAgent.occludedObjectOverTime.Count + " / " + student.totalStep);

                        }

                        // collect
                        Metrics met = new Metrics();
                        met.trackId = currentScene;
                        met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
                        met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
                        met.labelPositions = labelPositions;
                        met.labelDistToTarget = labelDistToTarget;

                        // save 
                        using (StreamWriter writer = new StreamWriter("student_track" + currentScene + "_met.json", false))
                        {
                            writer.Write(JsonUtility.ToJson(met));
                            writer.Close();
                        }
                    }
                }
                // replay the scene
                LoadScene(currentScene);
            }
        }
    }

    protected override void LoadDataset()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, "student_full.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();
        string[] records = pos_data.Split('\n');

        List<Dictionary<int, List<Vector3>>> tracks = new List<Dictionary<int, List<Vector3>>>();
        Dictionary<string, int> playerStartedInTrack = new Dictionary<string, int>();
        
        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int trackIdx = int.Parse(array[0]);
            int currentStep = int.Parse(array[1]);
            int playerIdx = int.Parse(array[2]);
            float px = float.Parse(array[3]);
            float py = float.Parse(array[4]);

            if ((trackIdx + 1) > tracks.Count) tracks.Add(new Dictionary<int, List<Vector3>>());
            var track = tracks[trackIdx];

            if (!track.ContainsKey(playerIdx))
            {
                track[playerIdx] = new List<Vector3>();
                playerStartedInTrack[trackIdx.ToString() + '_' + playerIdx.ToString()] = currentStep;
            }
            var playerPos = track[playerIdx];
            playerPos.Add(new Vector3(px, 0.5f, py));
        }

        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        for(int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            List<Student> track = new List<Student>();
            foreach(var entry in tracks[tIdx])
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
                if(pos.Length > 1)
                    vel[pos.Length - 1] = vel[pos.Length - 2];

                Student student = new Student();
                student.id = playerIdx;
                student.positions = pos;
                student.velocities = vel;
                student.startStep = playerStartedInTrack[tIdx.ToString() + '_' + playerIdx.ToString()];
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
}
