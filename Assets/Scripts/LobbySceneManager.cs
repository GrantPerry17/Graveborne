using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Realtime;
using System.Text;
using TMPro;

public class LobbySceneManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private TMP_InputField inputRoomName;

    [SerializeField]
    private TMP_InputField inputPlayerName;

    [SerializeField]
    private TextMeshProUGUI textRoomList;

    void Start()
    {
        if (PhotonNetwork.IsConnected == false)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            if (PhotonNetwork.CurrentLobby == null)
            {
                PhotonNetwork.JoinLobby();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        print("Connected to Master!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        print("Lobby joined.");
    }

    public string GetRoomName()
    {
        string roomName = inputRoomName.text;
        return roomName.Trim();
    }

    public string GetPlayerName()
    {
        string playerName = inputPlayerName.text;
        return playerName.Trim();
    }

    public void OnClickCreateRoom()
    {
        string playerName = GetPlayerName();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("Player Name is invalid.");
            return;
        }

        PhotonNetwork.LocalPlayer.NickName = playerName;

        string roomName = GetRoomName();
        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.CreateRoom(roomName);
        }
    }

    public void OnClickJoinRoom()
    {
        string playerName = GetPlayerName();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("Player Name is invalid.");
            return;
        }

        PhotonNetwork.LocalPlayer.NickName = playerName;

        string roomName = GetRoomName();
        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    public override void OnJoinedRoom()
    {
        print("Room Joined!");
        SceneManager.LoadScene("RoomScene");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        StringBuilder sb = new StringBuilder();

        foreach (RoomInfo roomInfo in roomList)
        {
            if (roomInfo.PlayerCount > 0)
            {
                sb.AppendLine($"RoomName: {roomInfo.Name}  Player Count: {roomInfo.PlayerCount}");
            }
        }

        textRoomList.text = sb.ToString();
    }

    public void OnClickBack()
    {
        print("Back to StartScene");

        // Disconnect from Photon so StartScene can connect again
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        SceneManager.LoadScene("StartScene");
    }

}
