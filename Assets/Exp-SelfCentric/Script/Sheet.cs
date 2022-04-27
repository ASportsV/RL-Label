using System;
using System.IO;
using System.Text;
using System.Linq;
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
        WWW reader = new WWW(fileName);
        while (!reader.isDone) {}
        jsonCred = new MemoryStream( Encoding.UTF8.GetBytes( reader.text ) );
#endif

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

    string getTechName(Tech tech)
    {
        return tech == Tech.No
        ? "No"
        : tech == Tech.Opti
        ? "Force"
        : "Ours";
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
            new List<object> { "TaskIdx", "Tech", "TrackIdx", "Task Type", "Q", "Corrected A", "Time", "User Ans"},
        };
        for(int i = 0, len = tasks.Count; i < len; ++i)
        {
            Task task = tasks[i];
            string tech = getTechName(order[i]);
            values.Add(
                new List<object> { 
                    i.ToString(), 
                    tech, 
                    task.track_id, 
                    task.type,
                    task.Q, 
                    task.A, 
                    "", 
                    "" }
            );
            if(i%3 == 2)
            {
                values.Add(
                    new List<object> { 
                        "A_" + getTechName(order[i-2]) + " rank:",
                        "",
                        "B_" + getTechName(order[i-1]) + " rank:",
                        "",
                        "C_" + getTechName(order[i])   + " rank:",
                        "",
                        "Why?",
                        ""
                    }
                );
            }
        }
        var valueRange = new ValueRange { 
            Values = values
        };
        var update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, WriteRange);
        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        update.Execute();


        Spreadsheet spr = service.Spreadsheets.Get(spreadsheetId).Execute();
        Sheet sh = spr.Sheets.Where(s => s.Properties.Title == sheetName).FirstOrDefault();
        int sheetId = (int)sh.Properties.SheetId;
        //define cell color
        var userEnteredFormat = new CellFormat()
        {
            TextFormat = new TextFormat()
            {
                Bold = true,
                FontSize = 11
            }
        };
        BatchUpdateSpreadsheetRequest bussr = new BatchUpdateSpreadsheetRequest();
        //create the update request for cells from the first row
        var updateCellsRequest = new Request()
        {
            RepeatCell = new RepeatCellRequest()
            {
                Range = new GridRange()
                {
                    SheetId = sheetId,
                    StartColumnIndex = 0,
                    StartRowIndex = 0,
                    EndColumnIndex = 28,
                    EndRowIndex = 1
                },
                Cell = new CellData()
                {
                    UserEnteredFormat = userEnteredFormat
                },
                Fields = "UserEnteredFormat(TextFormat)"
            }
        };
        bussr.Requests = new List<Request>();
        bussr.Requests.Add(updateCellsRequest);
        for(int i = 0; i < 6; ++i)
        {
            for(int j = 0; j < 4; ++j)
            {
                updateCellsRequest = new Request()
                {
                    RepeatCell = new RepeatCellRequest()
                    {
                        Range = new GridRange()
                        {
                            SheetId = sheetId,
                            StartColumnIndex = j * 2,
                            StartRowIndex = i * 4 + 4,
                            EndColumnIndex = j * 2 + 1,
                            EndRowIndex = i * 4 + 4 + 1
                        },
                        Cell = new CellData()
                        {
                            UserEnteredFormat = userEnteredFormat
                        },
                        Fields = "UserEnteredFormat(TextFormat)"
                    }
                };
                bussr.Requests.Add(updateCellsRequest);
            }
        }
        batchUpdateRequest = service.Spreadsheets.BatchUpdate(bussr, spreadsheetId);
        batchUpdateRequest.Execute();
    }

    public void SetAns(string sheetName, int taskIdx, float time)
    {
        string WriteRange = sheetName + "!G" + (taskIdx+2 + taskIdx / 3);
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