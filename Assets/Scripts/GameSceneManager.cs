using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using ExitGames.Client.Photon;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI aliveCountText;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Gameplay Settings")]
    [SerializeField] private float roundDuration = 60f;

    private PhotonView _pv;
    public Dictionary<Player, bool> alivePlayerMap = new Dictionary<Player, bool>();

    private bool roundActive = false;
    private float roundTimeLeft;
    private double roundStartTime;

    private List<string> messageList = new List<string>();

    void Start()
    {
        _pv = GetComponent<PhotonView>();

        if (PhotonNetwork.CurrentRoom == null)
        {
            SceneManager.LoadScene("LobbyScene");
            return;
        }

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        InitGame();
    }

    void Update()
    {
        // ESC key quits back to RoomScene
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void InitGame()
    {
        alivePlayerMap.Clear();
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
            alivePlayerMap[player] = true;

        // Spawn local player
        float spawnX = Random.Range(-5f, 5f);
        float spawnY = 1f;
        PhotonNetwork.Instantiate("PhotonPlayer", new Vector3(spawnX, spawnY, 0), Quaternion.identity);

        UpdateAliveCountUI();

        // Sync timer
        if (PhotonNetwork.IsMasterClient)
        {
            roundStartTime = PhotonNetwork.Time;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "RoundStartTime", roundStartTime }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("RoundStartTime"))
                roundStartTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["RoundStartTime"];
            else
                roundStartTime = PhotonNetwork.Time;
        }

        roundActive = true;
        StartCoroutine(RoundTimer());
    }

    IEnumerator RoundTimer()
    {
        while (roundActive)
        {
            double elapsed = PhotonNetwork.Time - roundStartTime;
            roundTimeLeft = roundDuration - (float)elapsed;

            if (roundTimeLeft <= 0f)
            {
                roundTimeLeft = 0f;
                roundActive = false;

                if (PhotonNetwork.IsMasterClient)
                    CheckGameOver();
            }

            UpdateTimerUI();
            yield return null;
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(roundTimeLeft / 60f);
            int seconds = Mathf.FloorToInt(roundTimeLeft % 60f);
            timerText.text = $"Time Left: {minutes:00}:{seconds:00}";
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        alivePlayerMap[newPlayer] = true;
        UpdateAliveCountUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (alivePlayerMap.ContainsKey(otherPlayer))
            alivePlayerMap.Remove(otherPlayer);

        UpdateAliveCountUI();

        // If only 1 player remains, master leaves room (ending game)
        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            PhotonNetwork.LeaveRoom();

        if (PhotonNetwork.IsMasterClient)
            CheckGameOver();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Master left — all players return to RoomScene
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("RoomScene");
    }

    public void CallRpcLocalPlayerDead()
    {
        _pv.RPC("RpcLocalPlayerDead", RpcTarget.All);
    }

    [PunRPC]
    void RpcLocalPlayerDead(PhotonMessageInfo info)
    {
        if (alivePlayerMap.ContainsKey(info.Sender))
            alivePlayerMap[info.Sender] = false;

        UpdateAliveCountUI();

        if (PhotonNetwork.IsMasterClient)
            CheckGameOver();
    }

    bool CheckGameOver()
    {
        int aliveCount = 0;
        foreach (bool alive in alivePlayerMap.Values)
            if (alive) aliveCount++;

        UpdateAliveCountUI();

        if (aliveCount <= 1 || roundTimeLeft <= 0f)
        {
            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
                gameOverText.text = "GAME OVER!";
            }

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(DelayedReload(1f));

            return true;
        }

        return false;
    }

    IEnumerator DelayedReload(float delay)
    {
        yield return new WaitForSeconds(delay);

        roundStartTime = PhotonNetwork.Time;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "RoundStartTime", roundStartTime }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        CallRpcReloadGame();
    }

    void UpdateAliveCountUI()
    {
        if (aliveCountText != null)
        {
            int count = 0;
            foreach (bool alive in alivePlayerMap.Values)
                if (alive) count++;

            aliveCountText.text = $"Players Alive: {count}";
        }
    }

    public void CallRpcSendMessageToAll(string message)
    {
        _pv.RPC("RpcSendMessage", RpcTarget.All, message);
    }

    [PunRPC]
    void RpcSendMessage(string message, PhotonMessageInfo info)
    {
        if (messageList.Count >= 10)
            messageList.RemoveAt(0);

        messageList.Add($"<b><color=#00FF99>[{info.Sender.NickName}]</color></b>: {message}");
        if (messageText != null)
            messageText.text = string.Join("\n", messageList);
    }

    void CallRpcReloadGame()
    {
        _pv.RPC("ReloadGame", RpcTarget.All);
    }

    [PunRPC]
    void ReloadGame(PhotonMessageInfo info)
    {
        PhotonNetwork.LoadLevel("GameScene");
    }
}
