using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PBPText : MonoBehaviour
{
    public float gameClock = 720;
    public int gameQuarter = 1;
    public GameObject gameTime;
    public TextAsset pbp_data;

    private int index = 0;

    private PBP pbpList;
    private struct PBP
    {
        public string[][] shotEvent;
        public string[] playEvent;
        public Vector2[] score;
        public float[] time;
        // Below are columns in PBP data
        //    AwayPlay    AwayScore HomeTeam    HomePlay HomeScore   
        //    Shooter ShotType    ShotOutcome ShotDist   
        //    Assister Blocker FoulType Fouler  Fouled Rebounder   ReboundType 
        //    ViolationPlayer ViolationType TimeoutTeam 
        //    FreeThrowShooter FreeThrowOutcome    FreeThrowNum 
        //    EnterGame   LeaveGame TurnoverPlayer 
        //    TurnoverType TurnoverCause  TurnoverCauser 
        //    JumpballAwayPlayer  JumpballHomePlayer JumpballPoss

    }
    void Start()
    {
        LoadData();
    }

    // Update is called once per frame
    void Update()
    {
        // update game clock
        string currentTime = ConvertTime(gameClock);
        gameTime.transform.Find("t1").GetComponentInChildren<Text>().text = currentTime[0].ToString();
        gameTime.transform.Find("t2").GetComponentInChildren<Text>().text = currentTime[1].ToString();
        gameTime.transform.Find("t3").GetComponentInChildren<Text>().text = currentTime[2].ToString();
        gameTime.transform.Find("t4").GetComponentInChildren<Text>().text = currentTime[3].ToString();

        if (pbpList.time[index] > gameClock && pbpList.time[index + 1] < gameClock && (pbpList.time[index] - gameClock) < 3)
        {
            // display game event
            gameObject.GetComponent<Text>().text = pbpList.playEvent[index];
            // highlight shooter
            //print("shooter" + pbpList.shotEvent[index][0]);
        } else if(pbpList.time[index + 1] > gameClock)
        {
            index = index + 1;
        }
        else
        {
            gameObject.GetComponent<Text>().text = "";
        }
    }
    private string ConvertTime(float gameClock)
    {
        int minute = (int)Math.Floor(gameClock / 60);
        string minuteStr = minute < 10 ? "0" + minute.ToString() : minute.ToString();
        int second = (int)Math.Floor(gameClock % 60);
        string secStr = second < 10 ? "0" + second.ToString() : second.ToString(); ;
        
        return minuteStr + secStr;
    }
    private void LoadData()
    {
        //TextAsset pbp_data = Resources.Load<TextAsset>("12.11.2015.GSW.at.BOS_pbp");

        string[] records = pbp_data.text.Split('\n');

        string[] playEvent = new string[records.Length];
        string[][] shotEvent = new string[records.Length][];
        float[] time = new float[records.Length];

        for (int i = 1; i < records.Length - 1; i++)
        {
            string[] array = records[i].Split(',');
            playEvent[i] = array[9]=="" ? array[12] : array[9];
            time[i] = float.Parse(array[7]);
            shotEvent[i] = new string[] { array[14], array[15], array[16], array[17] };
        }
        pbpList.playEvent = playEvent;
        pbpList.shotEvent = shotEvent;
        pbpList.time = time;
    }
}
