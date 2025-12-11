using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// Manages the initial scene interactions, specifically connecting to the Photon Master Server.
/// It inherits from MonoBehaviourPunCallbacks to get access to Photon's callback methods.
/// </summary>
public class StartSceneManager : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// Called by a UI Button to start connecting to Photon.
    /// </summary>
    public void OnClickStart()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        print("ClickStart");
    }

    /// <summary>
    /// Called automatically after connecting to the Master Server.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        print("Connected!");
        SceneManager.LoadScene("LobbyScene");
    }

    /// <summary>
    /// Called by a UI Button to quit the application.
    /// </summary>
    public void OnClickQuit()
    {
        print("Quit button pressed!");

        // If running in a built game
        Application.Quit();

#if UNITY_EDITOR
        // If testing in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
