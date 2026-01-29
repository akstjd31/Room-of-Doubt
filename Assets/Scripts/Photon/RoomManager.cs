using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button startButton;

    private void Start()
    {
        // if (!PhotonNetwork.InRoom)
        // {
        //     Debug.LogWarning("[Room] Not in room. Go back to LobbyScene.");
        //     PhotonNetwork.LoadLevel("LobbyScene");
        //     return;
        // }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (startButton != null)
            startButton.interactable = PhotonNetwork.IsMasterClient;

        foreach (var p in PhotonNetwork.PlayerList)
            Debug.Log("방 사람들 목록: " + p.NickName);
    }

    // 버튼 이벤트
    public void StartButton()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;

        // S == Started
        var props = new ExitGames.Client.Photon.Hashtable { { "S", true } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        PhotonNetwork.LoadLevel("InGameScene");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[Room] OnJoinedRoom");
        RefreshUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " 님이 방에 입장하셨습니다.");
        RefreshUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(otherPlayer.NickName + " 님이 나갔습니다.");
        RefreshUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log(newMasterClient.NickName + " 님이 새로운 방장이 되었습니다.");
        RefreshUI();
    }
}
