using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button startButton;
    private const int MAX_PLAYER = 4;

    private void Awake()
    {
        StartCoroutine(JoinFlow());
    }

    private IEnumerator JoinFlow()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

        // 확실한 로비다 라는 보장을 주기 위한 세이프 코드
        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();

        yield return new WaitUntil(() => PhotonNetwork.InLobby);
        PhotonNetwork.JoinRandomOrCreateRoom(expectedMaxPlayers: MAX_PLAYER);
    }

    public void StartButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            
            // S == Started, 시작!
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "S", true } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            PhotonNetwork.LoadLevel("InGameScene");
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방에 입장하셨습니다.");

        foreach (var p in PhotonNetwork.PlayerList)
            Debug.Log("방 사람들 목록: " + p.NickName);

        // 방 시작 버튼은 마스터 클라이언트(방장)에게만 권한이 있음.
        startButton.interactable = PhotonNetwork.IsMasterClient;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"랜덤 입장 실패: {returnCode}: {message}");

        // 재시도?? 로직은 고민
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"방 생성 실패: {returnCode}: {message}");

        // 재시도?? 로직은 고민
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("연결 끊김: " + cause);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " 님이 방에 입장하셨습니다.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(otherPlayer.NickName + " 님이 나갔습니다.");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log(newMasterClient.NickName + " 님이 새로운 방장이 되었습니다.");

        // 방장 바뀜 == 시작버튼 권한 양도
        startButton.interactable = PhotonNetwork.IsMasterClient;
    }
}
