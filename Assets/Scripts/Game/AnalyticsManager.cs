using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private string sessionID;
    private DateTime matchStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeAnalytics();
    }

    private async void InitializeAnalytics()
    {
        sessionID = Guid.NewGuid().ToString();
        try
        {
            await UnityServices.InitializeAsync();
            AnalyticsService.Instance.StartDataCollection();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    public void LogMatchStart()
    {
        matchStartTime = DateTime.UtcNow;
        CustomEvent eventData = new CustomEvent("match_start")
        {
            { "session_id", sessionID },
            { "timestamp_string", matchStartTime.ToString("o") }
        };
        SendEvent(eventData);
    }

    public void LogMatchEnd(string result)
    {
        DateTime endTime = DateTime.UtcNow;
        TimeSpan duration = endTime - matchStartTime;
        CustomEvent eventData = new CustomEvent("match_end")
        {
            { "session_id", sessionID },
            { "timestamp_string", endTime.ToString("o") },
            { "duration_seconds", duration.TotalSeconds },
            { "result", result }
        };
        SendEvent(eventData);
    }

    public void LogDLCPurchase(string dlcName, int price)
    {
        CustomEvent eventData = new CustomEvent("dlc_purchase")
        {
            { "session_id", sessionID },
            { "timestamp_string", DateTime.UtcNow.ToString("o") },
            { "dlc_name", dlcName },
            { "price", price }
        };
        SendEvent(eventData);
    }

    private void SendEvent(CustomEvent eventData)
    {
        try
        {
            AnalyticsService.Instance.RecordEvent(eventData);
            Debug.Log("Event logged successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to log event. Error: {e.Message}");
        }
    }
}