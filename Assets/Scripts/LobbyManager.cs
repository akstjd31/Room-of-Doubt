using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public override void OnJoinedLobby()
    {
        Debug.Log("로비에 입장하셨습니다.");
    }

    // 버튼 이벤트 (빠참)
    public void RandomOrCreateRoom()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
        SceneManager.LoadScene("RoomScene");
    }
}
