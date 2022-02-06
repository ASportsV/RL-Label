using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;


public class createObj : MonoBehaviour
{
    // json data
    private List<JsonItem> jsonItems = new List<JsonItem>();

    // ball
    public GameObject ball_prefab;
    // player
    public GameObject player_prefab;
    // label
    public GameObject label_prefab;

    //public Material Red;
    public List<Color> colors = new List<Color> { Color.blue, Color.red };

    // const
    private string homeTeam = "GSW";
    
    // time control
    private int initStep = 7501;
    public int totalStep = 0;
    private float timeStep = 0.04f;

    // Start is called before the first frame update
    void Start()
    {
        string fileName = Path.Combine(Application.dataPath, "Scripts/data_list.json");
        StreamReader r = new StreamReader(fileName);
        string json = r.ReadToEnd();
        jsonItems = JsonUtility.FromJson<ListItem>(json).Objects.ToList();
        int count = 0;
        for (int i = 0; i < jsonItems.Count; ++i)
        {
            var exist = Resources.Load<TextAsset>("12.25.2015.CLE.at.GSW/clean_pos_data_" + jsonItems[i].Text);
            if (exist == null) continue;

            if(jsonItems[i].Team == "ball")
            {
                //this.CreateBall(jsonItems[i]);
            }
            else // if(jsonItems[i].Text == "Draymond Green")
            {
                var player = this.CreatePlayer(jsonItems[i]);
                this.CreateLabel(player);
                ++count;
            }
        }
    }

    /**==================== Ball ================== */
    public void CreateBall(JsonItem data) {
        Vector3[] positions = GetPos(data, true);
        Vector3[] velocities = GetVels(positions);

        GameObject newObj = Instantiate(ball_prefab, positions[initStep], Quaternion.identity);
        newObj.transform.SetParent(gameObject.transform, false);
        newObj.name = data.Text;

        var playerData = newObj.GetComponent<EnvObj>();
        playerData.positions = positions;
        playerData.velocities = velocities;
        playerData.totalStep = totalStep;
        playerData.SetInitStep(initStep);
    }

    /**==================== Player ================== */
    public GameObject CreatePlayer(JsonItem data) {
        Vector3[] positions = GetPos(data);
        Vector3[] velocities = GetVels(positions);
        bool isHomeTeam = data.Team == homeTeam;

        GameObject playerObj = Instantiate(player_prefab, positions[initStep], Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = data.Text;

        var playerData = playerObj.GetComponent<EnvObj>();
        playerData.positions = positions;
        playerData.velocities = velocities;
        playerData.totalStep = totalStep;
        playerData.SetInitStep(initStep);

        Transform cam = this.transform.parent.Find("Camera");
        playerData.sceneCamera = cam;
        playerData.overlay = this.transform.parent.Find("Canvas");

        Color color = isHomeTeam ? colors[0] : colors[1];

        //Get the Renderer component from the new cube
        var cubeRenderer = playerObj.GetComponent<Renderer>();
        cubeRenderer.material.SetColor("_Color", color);

        return playerObj;
    }

    /**==================== Label ================== */
    public void CreateLabel(GameObject playerObj) {

        GameObject arLabel = Instantiate(label_prefab, playerObj.transform.position, Quaternion.identity) as GameObject;
        arLabel.transform.SetParent(gameObject.transform, false);
        arLabel.name = "label_" + playerObj.name;
        arLabel.GetComponentInChildren<Text>().text = playerObj.name;

        var labelAgent = arLabel.GetComponent<LabelAgent>();
        Transform cam = this.transform.parent.Find("Camera");
        labelAgent.sceneCamera = cam;
        labelAgent.overlay = this.transform.parent.Find("Canvas");

        EnvObj player = playerObj.GetComponent<EnvObj>();
        labelAgent.player = playerObj;
        labelAgent.totalStep = totalStep;
        labelAgent.initStep = initStep;
        arLabel.GetComponent<Rigidbody>().velocity = playerObj.GetComponent<Rigidbody>().velocity;
        
        Vector3 dSpeed = Vector3.zero;
        for(int i = initStep+1; i < totalStep; ++i)
        {
            var dv = player.velocities[i] - player.velocities[i - 1];
            dSpeed.x = Math.Max(dSpeed.x, Math.Abs(dv.x));
            dSpeed.y = Math.Max(dSpeed.y, Math.Abs(dv.y));
            dSpeed.z = Math.Max(dSpeed.z, Math.Abs(dv.z));
        }
        //print("DSpeed of " + playerObj.name + " " + ix + "_" + dSpeed.x + ", " + iy + "_" + dSpeed.y + ", " + iz + "_" + dSpeed.z);
        labelAgent.maxAcc = dSpeed / timeStep;

        Vector3 maxSpeed = Vector3.zero;
        for(int i = initStep; i < totalStep; ++i)
        {
            var dv = player.velocities[i];
            maxSpeed.x = Math.Max(maxSpeed.x, Math.Abs(dv.x));
            maxSpeed.y = Math.Max(maxSpeed.y, Math.Abs(dv.y));
            maxSpeed.z = Math.Max(maxSpeed.z, Math.Abs(dv.z));
        }
        labelAgent.maxSpeed = maxSpeed;
    }
    
    private Vector3[] GetPos(JsonItem item, bool isBall = false)
    {
        var pos_data = Resources.Load<TextAsset>("12.25.2015.CLE.at.GSW/clean_pos_data_" + item.Text);

        string[] records = pos_data.text.Split('\n');
        totalStep = totalStep == 0 ? records.Length - 1 : totalStep;
        Vector3[] steps = new Vector3[totalStep];

        for (int i = 1; i < records.Length; i++)
        {
            string[] array = records[i].Split(',');
            //steps[i - 1] = new Vector3(UnitConvert(float.Parse(array[0])), isBall ? 1.8f : 0.4f, UnitConvert(float.Parse(array[1])));
            steps[i - 1] = new Vector3(UnitConvert(float.Parse(array[0])), isBall ? 1.4f : 0.5f, UnitConvert(float.Parse(array[1])));
        }
        return steps;
    }

    private Vector3[] GetVels(Vector3[] positions)
    {
        Vector3[] velocities = new Vector3[positions.Length];
        for(int i = 0; i < positions.Length - 1; ++i)
        {
            Vector3 cur = positions[i];
            Vector3 next = positions[i + 1];
            Vector3 vel = (next - cur) / timeStep;
            velocities[i] = vel;
        }
        velocities[positions.Length - 1] = Vector3.zero;
        return velocities;
    }
    private float UnitConvert(float v)
    {
        return v * 0.3048f;
    }
}

[Serializable]
public class ListItem
{
    public JsonItem[] Objects;
}
[Serializable]
public class JsonItem
{
    public string Text;
    public float Value;
    public string Team;
    public List<Vector3>[] Steps;
}
