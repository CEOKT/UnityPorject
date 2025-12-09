using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIClient : MonoBehaviour
{
    public static AIClient Instance { get; private set; }
    
    public string serverUrl = "http://127.0.0.1:1234/v1/chat/completions";
    public string modelName = "google/gemma-3-4b";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public IEnumerator AskLLM(string systemPrompt, string userMessage, System.Action<string> callback)
    {
        // JSON'u manuel oluştur (JsonUtility anonim objelerle çalışmıyor)
        string json = BuildRequestJson(systemPrompt, userMessage);
        Debug.Log("[AIClient] Sending: " + json);

        using (UnityWebRequest req = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AIClient] Error: " + req.error);
                callback("...");
            }
            else
            {
                string response = req.downloadHandler.text;
                Debug.Log("[AIClient] Raw response: " + response);
                
                // JSON parse et ve content'i çıkar
                string content = ParseResponse(response);
                callback(content);
            }
        }
    }

    private string BuildRequestJson(string systemPrompt, string userMessage)
    {
        // Escape special characters
        systemPrompt = EscapeJson(systemPrompt);
        userMessage = EscapeJson(userMessage);

        return $@"{{
            ""model"": ""{modelName}"",
            ""messages"": [
                {{""role"": ""system"", ""content"": ""{systemPrompt}""}},
                {{""role"": ""user"", ""content"": ""{userMessage}""}}
            ],
            ""max_tokens"": 200,
            ""temperature"": 0.7,
            ""top_p"": 0.9
        }}";
    }

    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private string ParseResponse(string json)
    {
        try
        {
            // Basit parse - "content": "..." kısmını bul
            int contentStart = json.IndexOf("\"content\":");
            if (contentStart == -1) return "...";

            contentStart = json.IndexOf("\"", contentStart + 10) + 1;
            int contentEnd = json.IndexOf("\"", contentStart);
            
            // Escaped quotes'u handle et
            while (contentEnd > 0 && json[contentEnd - 1] == '\\')
            {
                contentEnd = json.IndexOf("\"", contentEnd + 1);
            }

            if (contentEnd > contentStart)
            {
                string content = json.Substring(contentStart, contentEnd - contentStart);
                // Unescape
                content = content.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
                return content;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AIClient] Parse error: " + e.Message);
        }
        return "...";
    }
}
