using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void ConnectToServer()
    {   
        // 연달아 클릭 방지용
        if (PhotonNetwork.IsConnected)
            return;

        // 그럼 유저한테 연결중이다는 문구가 필요하겠지? => 일단 나중에 추가
        Debug.Log("서버 연결 시도 중..");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결 완료!");
        SceneManager.LoadScene("LobbyScene");
    }
}
