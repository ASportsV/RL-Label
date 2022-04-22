using System;
using System.IO;
using System.Collections.Generic;

using Google.Apis.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;


using UnityEngine;

class SheetReader
{
    static private String spreadsheetId = "1VI9vcTGRZv6FkIrOqxiy_3DCJA3JtLgjpOJlKNjqMuI";

    static private String serviceAccountID = "vcg-sportsxr@vcg-project-348015.iam.gserviceaccount.com";
    static private SheetsService service;

    static SheetReader()
    {
        //  Loading private key from resources as a TextAsset
        //String key = Resources.Load<TextAsset>("Creds/key").ToString();

        string fileName = Path.Combine(Application.streamingAssetsPath, "vcg-project-348015-b710435e7ac2.json");
        Stream jsonCred;
#if UNITY_EDITOR || !UNITY_ANDROID
        jsonCred = (Stream)File.Open(fileName, FileMode.Open);
#else
    // streamingAssets are compressed in android (not readable with File).
        jsonCred = (Stream) new WWW (fileName);
        //while (!reader.isDone) {}
        //key = reader.text;
#endif

        //Debug.Log(key);

        //// Creating a  ServiceAccountCredential.Initializer
        //// ref: https://googleapis.dev/dotnet/Google.Apis.Auth/latest/api/Google.Apis.Auth.OAuth2.ServiceAccountCredential.Initializer.html
        //ServiceAccountCredential.Initializer initializer = new ServiceAccountCredential.Initializer(serviceAccountID);

        //// Getting ServiceAccountCredential from the private key
        //// ref: https://googleapis.dev/dotnet/Google.Apis.Auth/latest/api/Google.Apis.Auth.OAuth2.ServiceAccountCredential.html
        //ServiceAccountCredential credential = new ServiceAccountCredential(
        //    initializer.FromPrivateKey(key)
        //);
        ServiceAccountCredential credential = ServiceAccountCredential.FromServiceAccountData(jsonCred);


        service = new SheetsService(
            new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
            }
        );
    }

    public IList<IList<object>> getSheetRange(String sheetNameAndRange)
    {
        SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, sheetNameAndRange);
        ValueRange response = request.Execute();
        IList<IList<object>> values = response.Values;
        if (values != null && values.Count > 0)
        {
            return values;
        }
        else
        {
            Debug.Log("No data found.");
            return null;
        }
    }

    public void AddNewSheet(string sheetName, List<Tech> order, List<Task> tasks)
    {
        // Add new Sheet
        var addSheetRequest = new AddSheetRequest();
        addSheetRequest.Properties = new SheetProperties();
        addSheetRequest.Properties.Title = sheetName;
        BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
        batchUpdateSpreadsheetRequest.Requests = new List<Request>();
        batchUpdateSpreadsheetRequest.Requests.Add(new Request
        {
            AddSheet = addSheetRequest,
        });
        var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);
        batchUpdateRequest.Execute();

        string WriteRange = sheetName + "!A1";
        var values = new List<IList<object>> {
            new List<object> { "TaskIdx", "Tech", "TrackIdx", "Q", "Corrected A", "Time", "User Ans"},
        };
        for(int i = 0, len = tasks.Count; i < len; ++i)
        {
            Task task = tasks[i];
            string tech = order[i] == Tech.No
                    ? "No"
                    : order[i] == Tech.Opti
                    ? "Force"
                    : "Ours";
            values.Add(
                new List<object> { i.ToString(), tech, task.track_id, task.Q, task.A, "", "" }
            );
        }
        var valueRange = new ValueRange { 
            Values = values
        };
        var update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, WriteRange);
        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        update.Execute();
    }

    public void SetAns(string sheetName, int taskIdx, float time)
    {
        string WriteRange = sheetName + "!F" + (taskIdx+2);
        var valueRange = new ValueRange
        {
            Values = new List<IList<object>> {
            new List<object> { time.ToString("F")},
        }
        };
        var update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, WriteRange);
        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        update.Execute();
    }
}