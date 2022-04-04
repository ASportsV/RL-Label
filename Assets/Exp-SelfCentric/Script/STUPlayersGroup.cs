using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

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

    private void FixedUpdate()
    {
        if (m_RVOSettings.sceneFinished || !m_RVOSettings.sceneStarted) return;

        time += Time.fixedDeltaTime;
        if(time < timeStep) return;
        // update step
        time -= timeStep;
        currentStep += 1;

        var players = scenes[currentScene];

        // add or move the student
        foreach(var player in players)
        {
            // enter a player
            if (currentStep == player.startStep)
            {
                var playerLab = CreatePlayerLabelFromPos(player);
                if(useBaseline) b.AddLabel(playerLab.Item1, playerLab.Item2);
            }
            // move a player
            else if (currentStep > player.startStep && currentStep < (player.startStep + player.totalStep))
            {
                m_playerMap[player.id].step(currentStep - player.startStep);
            }
            // exit a player
            else if (currentStep >= (player.startStep + player.totalStep) && m_playerMap[player.id].gameObject.activeSelf)
            {
                var go = m_playerMap[player.id].gameObject;
                // save RL data
                if(!useBaseline)
                {
                    var labelAgent = go.GetComponentInChildren<RVOLabelAgent>();
                    labelAgent.SyncReset();
                }
                // deactivate
                go.SetActive(false);
                foreach (Transform child in go.transform)
                    child.gameObject.SetActive(false);
            }
        }

        // scene finished
        if (currentStep >= totalStep)
        {
            if(!useBaseline && m_RVOSettings.evaluate)
            {
                // save metrics
                SaveMetricToJson("stu", totalStep, players);
            }
            // replay the scene
            LoadScene(currentScene);
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
            List<PlayerData> track = new List<PlayerData>();
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

                PlayerData student = new PlayerData();
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
