using System.Linq;
using UnityEngine;

public class TESTGroup : NBAPlayerGroup
{

    protected override void LoadScene(int sceneIdx)
    {
        Debug.Log("TESTGroup LoadScene");
        Clean();
        currentScene = 0; // sceneIdx;
        currentStep = 0;

        var students = scenes[currentScene];
        int agentIdx = Random.Range(0, students.Count());
        //foreach(var student in students)
        //for (int i = 0, len = students.Count(); i < len; ++i)
        //{
        var student = students[0];
        if (currentStep == student.startStep)
        {
            CreatePlayerLabelFromPos(student, true);
        }
        //}
        totalStep = students.Max(s => s.startStep + s.totalStep);
    }

}
