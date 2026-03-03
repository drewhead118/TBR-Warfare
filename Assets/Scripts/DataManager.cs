using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Threading;

[Serializable]
public class ArmyComp { public float k, a, m; }

[Serializable]
public class BookData {
    public string id; public string title; public string coverUrl;
    public int armySize; public string format; // "digital" or "paperback"
    public ArmyComp comp;
}

[Serializable]
public class Database { public List<BookData> books = new List<BookData>(); }

public class DataManager : MonoBehaviour {
    public static DataManager Instance;
    public Database db = new Database();
    private string savePath;

    // API Server fields
    private HttpListener listener;
    private Thread listenerThread;
    private bool serverRunning;

    void Awake() {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        
        savePath = Application.persistentDataPath + "/tbr_data.json";
        LoadData();
        StartAPIServer();
    }

    public void SaveData() {
        File.WriteAllText(savePath, JsonUtility.ToJson(db, true));
    }

    public void LoadData() {
        if (File.Exists(savePath)) db = JsonUtility.FromJson<Database>(File.ReadAllText(savePath));
        else db.books = new List<BookData>(); // Default data can be added here
    }

    // --- DISCORD BOT API HOOK ---
    private void StartAPIServer() {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/tbr/"); // Bot will send requests here
        listener.Start();
        serverRunning = true;
        listenerThread = new Thread(ListenForRequests);
        listenerThread.Start();
        Debug.Log("API Server started on port 8080");
    }

    private void ListenForRequests() {
        while (serverRunning) {
            try {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                
                if (request.HttpMethod == "POST") {
                    using (StreamReader reader = new StreamReader(request.InputStream)) {
                        string json = reader.ReadToEnd();
                        // Example: Bot sends {"bookId": "123", "k": 2, "a": 1, "m": 0}
                        // You would parse this and update db.books here.
                        // Note: Threading! Use a concurrent queue to apply changes on the main thread.
                    }
                }
                
                HttpListenerResponse response = context.Response;
                string responseString = "{\"status\":\"success\"}";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            } catch (Exception) { }
        }
    }

    void OnApplicationQuit() {
        serverRunning = false;
        if (listener != null) listener.Stop();
        if (listenerThread != null) listenerThread.Abort();
    }
}