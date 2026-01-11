using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    private void Awake() => PhotonNetwork.AutomaticallySyncScene = true;
    public void ConnectToServer()
    {
        PhotonNetwork.ConnectUsingSettings();
        SceneManager.LoadScene("LobbyScene");
        Debug.Log("서버 연결됨!");
    }
}
