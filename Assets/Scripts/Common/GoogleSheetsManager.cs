using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using ClickClick.Data;

namespace ClickClick.Manager
{
    public class GoogleSheetsManager : MonoBehaviour
    {
        [SerializeField] private bool isActive = false;
        private const string GOOGLE_SHEETS_URL = "https://docs.google.com/spreadsheets/d/e/2PACX-1vSLsK_WH4savCI5APLz6fPFjbERNbsvjcvkIou1iNOY9oxLrC2l3UCsWJ3oVB2OQJLaV3x0mqXVrs_4/pub?gid=0&single=true&output=csv";
        private const string GOOGLE_FORM_URL = "https://docs.google.com/forms/d/1REAecoHDW6Tp25KPYGKfl4A6FdDKKildTzv28KOWpJ4/formResponse";

        public IEnumerator UploadPlayerData(PlayerData playerData)
        {
            if (!isActive) yield break;
            // Create form data
            WWWForm form = new WWWForm();
            form.AddField("entry.676096407", playerData.PlayerId.ToString());
            form.AddField("entry.2099973968", playerData.CharacterId.ToString());
            form.AddField("entry.531399903", playerData.Score.ToString());
            form.AddField("entry.654461480", playerData.Rank.ToString());

            using (UnityWebRequest www = UnityWebRequest.Post(GOOGLE_FORM_URL, form))
            {
                // Add headers to handle CORS
                www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                www.SetRequestHeader("Origin", "https://docs.google.com");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to upload player data: {www.error}");
                    Debug.LogError($"Response Code: {www.responseCode}");
                    Debug.LogError($"Response: {www.downloadHandler.text}");
                }
                else
                {
                    Debug.Log("Successfully uploaded player data to Google Sheets");
                }
            }
        }

        public IEnumerator GetAllPlayersData()
        {
            if (!isActive) yield break;

            using (UnityWebRequest www = UnityWebRequest.Get(GOOGLE_SHEETS_URL))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get players data: {www.error}");
                }
                else
                {
                    string csvData = www.downloadHandler.text;
                    ProcessCSVData(csvData);
                }
            }
        }

        private void ProcessCSVData(string csvData)
        {
            string[] lines = csvData.Split('\n');
            for (int i = 1; i < lines.Length; i++) // Skip header row
            {
                string[] values = lines[i].Split(',');
                if (values.Length >= 4)
                {
                    Debug.Log($"Player ID: {values[0]}, Character ID: {values[1]}, Score: {values[2]}, Rank: {values[3]}");
                }
            }
        }
    }
}