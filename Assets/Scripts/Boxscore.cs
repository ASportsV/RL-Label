using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class Boxscore : MonoBehaviour
{
    public List<BoxObjects> bsList = new List<BoxObjects>();
    // Start is called before the first frame update
    void Start()
    {
        string fileBoxScore = Path.Combine(Application.dataPath, "Scripts/boxscore_record.json");
        LoadBoxScoreJson(fileBoxScore);
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void LoadBoxScoreJson(string fileName)
    {
        using (StreamReader r = new StreamReader(fileName))
        {
            string json = r.ReadToEnd();
            BoxItem box = JsonUtility.FromJson<BoxItem>(json);
            bsList = box.BoxObjects.ToList();
        }
        //CreateBoxScore();
    }
    public void CreateBoxScore()
    {
        for (int i = 0; i < bsList.Count; i++)
        {

            //creat a player obj;
            print("Boxscore: " + bsList[i].SecLeft);
            print("player: " + bsList[i].players);
            for (int j = 0; j < bsList[i].players.Length; j++) {
                print("Boxscore player: " + bsList[i].players[j].name + " pts: " + bsList[i].players[j].PTS);
            }

        }
    }
}


[Serializable]
public class BoxItem
{
    public BoxObjects[] BoxObjects;
}
[Serializable]
public class BoxObjects
{
    public float Quarter;
    public float SecLeft;
    public players[] players;
}
[Serializable]
public class players
{
    public string name;
    public string role;
    public string type;
    public string team;
    public string FG;
    public string THREE;
    public string FT;
    public string OREB;
    public string DREB;
    public string REB;
    public string AST;
    public string STL;
    public string BLK;
    public string TO;
    public string PF;
    public string PTS;
}
