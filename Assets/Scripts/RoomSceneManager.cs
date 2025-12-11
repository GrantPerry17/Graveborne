using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Text;
using ExitGames.Client.Photon;

public class RoomSceneManager : MonoBehaviourPunCallbacks
{
    [SerializeField]
    private TextMeshProUGUI textRoomName;

    [SerializeField]
    private TextMeshProUGUI textPlayerList;

    [SerializeField]
    private Button buttonStartGame;

    [SerializeField]
    private Image panelToChangeColor;

    [SerializeField]
    private Button buttonReadyToggle;

    private bool isReady = false;

    void Start()
    {
        // Not in a room? Go back.
        if (PhotonNetwork.CurrentRoom == null)
        {
            SceneManager.LoadScene("LobbyScene");
            return;
        }

        textRoomName.text = "Room: " + PhotonNetwork.CurrentRoom.Name;

        // Initialize ready property
        ExitGames.Client.Photon.Hashtable myProps = new ExitGames.Client.Photon.Hashtable
        {
            { "IsReady", isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(myProps);

        // Reset ready status for all players (on returning from GameScene)
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.CustomProperties.ContainsKey("IsReady"))
            {
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "IsReady", false }
                };
                player.SetCustomProperties(props);
            }
        }

        UpdateReadyButtonText();
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Players:");

        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            bool ready = false;

            if (player.CustomProperties.ContainsKey("IsReady"))
                ready = (bool)player.CustomProperties["IsReady"];

            string colorTag = ready ? "<color=green>" : "<color=red>";
            sb.AppendLine($"{colorTag}- {player.NickName}</color>");
        }

        textPlayerList.text = sb.ToString();

        // Only master client can start and only if all ready
        if (buttonStartGame != null)
            buttonStartGame.interactable = PhotonNetwork.IsMasterClient && AreAllPlayersReady();
    }

    private bool AreAllPlayersReady()
    {
        foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.CustomProperties.ContainsKey("IsReady") ||
                !(bool)player.CustomProperties["IsReady"])
            {
                return false;
            }
        }
        return true;
    }

    public void OnClickToggleReady()
    {
        isReady = !isReady;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "IsReady", isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateReadyButtonText();
    }

    private void UpdateReadyButtonText()
    {
        if (buttonReadyToggle != null)
            buttonReadyToggle.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "Unready" : "Ready";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
            UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerList();

        if (buttonStartGame != null)
            buttonStartGame.interactable = PhotonNetwork.IsMasterClient && AreAllPlayersReady();
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
