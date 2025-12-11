using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class PingSceneManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI pingText;

    void Update()
    {
        if (PhotonNetwork.IsConnected)
        {
            int ping = PhotonNetwork.GetPing();
            pingText.text = $"Ping: {ping} ms";

            // Color code the ping
            if (ping < 100)
            {
                pingText.color = Color.green;  // Good
            }
            else if (ping < 200)
            {
                pingText.color = Color.yellow; // Moderate
            }
            else
            {
                pingText.color = Color.red;    // Poor
            }
        }
        else
        {
            pingText.text = "Not Connected";
            pingText.color = Color.gray;
        }
    }
}
