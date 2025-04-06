using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    private string sessionID = Guid.NewGuid().ToString();

    public void LogMatchStart()
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "session_id", sessionID },
            { "event_name", "match_start" },
            { "timestamp", DateTime.UtcNow.ToString("o") } //ISO 8601 timestamp
        };

        SendEvent("match_start", eventData);
    }

    public void LogMatchEnd(int score, float duration)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "session_id", sessionID },
            { "event_name", "match_end" },
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "score", score },
            { "duration_seconds", duration }
        };

        SendEvent("match_end", eventData);
    }

    public void LogDLCPurchase(string dlcName, float price, string currency)
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "session_id", sessionID },
            { "event_name", "dlc_purchase" },
            { "timestamp", DateTime.UtcNow.ToString("o") },
            { "dlc_name", dlcName },
            { "price", price },
            { "currency", currency }
        };

        SendEvent("dlc_purchase", eventData);
    }

    private void SendEvent(string eventName, Dictionary<string, object> data)
    {
        AnalyticsResult result = Analytics.CustomEvent(eventName, data);

        if (result == AnalyticsResult.Ok)
        {
            Debug.Log($"Event '{eventName}' logged successfully.");
        }
        else
        {
            Debug.LogError($"Failed to log event '{eventName}'. Error: {result}");
        }
    }
}