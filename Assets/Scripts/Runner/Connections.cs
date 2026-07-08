using UnityEngine;
using NativeWebSocket;
using UnityEngine.SceneManagement;
using System.Collections;

public class Connections : MonoBehaviour
{
    PlayerGenderController playerGenderController;
    WebSocket websocket;

    [SerializeField] private GameObject loadingScreen;

    [SerializeField] private GameObject resettingScreen;

    // True once the websocket has successfully connected. Used to gate the
    // space-bar "start" action so players can't start before the server link is up.
    public bool IsConnected { get; private set; } = false;

    public bool IsRestarting { get; private set; } = false;


    async void Start()
    {
        loadingScreen.SetActive(false);
        Application.runInBackground = true; // Recommended for WebGL
        websocket = new WebSocket("ws://192.168.1.200:3002");

        websocket.OnOpen += () =>
        {
            string message = JsonUtility.ToJson(new IdentifyMessage());
            websocket.SendText(message);
            Debug.Log("Connection open!");
        };
        websocket.OnError += (e) =>
        {
            IsConnected = false;
            Debug.Log("Error! " + e);
        };
        websocket.OnClose += (code) =>
        {
            IsConnected = false;
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            JSONItems jsonItems = JsonUtility.FromJson<JSONItems>(message);
            Debug.Log("OnMessage! " + message);
            string type = jsonItems.type;
            if (type == "start")
            {
                loadingScreen.SetActive(true);
                IsConnected = true;
                //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                string playerName = jsonItems.playerName;
                string avatar = jsonItems.avatar;
                Debug.Log("Player Name: " + playerName);
                Debug.Log("Avatar: " + avatar);


                if (avatar == "female")
                {
                    Debug.Log("madeit   " + avatar);
                    GameObject.Find("Player").GetComponent<PlayerGenderController>().selectFemale();
                }
                else if (avatar == "male")
                {
                    Debug.Log("madeit   " + avatar);
                    GameObject.Find("Player").GetComponent<PlayerGenderController>().selectMale();
                }
                else if (avatar == "other")
                {
                    Debug.Log("madeit   " + avatar);
                    GameObject.Find("Player").GetComponent<PlayerGenderController>().selectNonBinary();
                }
                RunnerGameManager.Instance?.SlideshowEnd();
                //RunnerGameManager.Instance?.StartGame();
                //loadingScreen.SetActive(false)
                //startPanel.SetActive(true);
            }
            if (type == "abort")
            {
                string score = jsonItems.score;
                Debug.Log("Score received: " + score);
                //RunnerGameManager.Instance?.EndGame(false); // Call EndGame with save = false
                immediateRestart(); // Start the delayed restart coroutine
            }

        };

        Debug.Log("attempting to conect to ...");

        await websocket.Connect();
    }

    private IEnumerator delayedRestart(float delay)
    {
        IsRestarting = true;


        yield return new WaitForSeconds(3f);
        resettingScreen.SetActive(true);
        yield return new WaitForSeconds(1f);



        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        IsRestarting = false;
    }
    private void immediateRestart()
    {
        IsRestarting = true;


        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        IsRestarting = false;

    }
    public async void SendWebSocketMessage(float score)
    {
        Debug.Log("Sending score: " + score);
        string message = JsonUtility.ToJson(new WebSocketMessage { score = (int)score, type = "end" });
        Debug.Log("Sending message: " + message);
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
        StartCoroutine(delayedRestart(3f)); // Start the delayed restart coroutine
    }




    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}

class WebSocketMessage
{
    public int score;
    public string type;
}

class JSONItems
{
    public string type;
    public string role;
    public string instance;
    public string playerName;
    public string avatar;
    public string score;
    public string message;
}
class IdentifyMessage
{
    public string type = "identify";
    public string role = "game";
    public string instance = "A"; // or "B"
}



/*
wsclient.send(
    JSON.stringify({
      type: "identify",
      role: "game",
      instance: "A", // or "B"
    }),
  );
*/