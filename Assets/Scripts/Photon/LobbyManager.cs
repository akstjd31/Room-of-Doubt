using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private FirebaseDBManager db;
    [SerializeField] private Button quickMachingbtn;

    private bool lobbyReady = false;

    private void Awake()
    {
        if (quickMachingbtn) quickMachingbtn.interactable = false;
    }

    private IEnumerator Start()
    {
        // 연결 대기
        yield return new WaitUntil(() => PhotonNetwork.IsConnected);

        // 로비에 있다면 준비 처리
        if (PhotonNetwork.InLobby)
        {
            lobbyReady = true;
            OnLobbyReady();
            yield break;
        }

        // 이미 서버 연결이 되어있다면 로비 입장 시도
        if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            JoinLobbySafe();
            yield break;
        }
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log($"마스터 서버에 연결됨. (콜백) 클라이언트 상태: {PhotonNetwork.NetworkClientState}");
        JoinLobbySafe();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("방에서 떠남!(콜백)");
    }

    private void JoinLobbySafe()
    {
        lobbyReady = false;

        Debug.Log($"로비 입장 시도! 클라이언트 상태: {PhotonNetwork.NetworkClientState}, 이미 로비인가?: {PhotonNetwork.InLobby}");

        if (PhotonNetwork.InLobby)
        {
            lobbyReady = true;
            OnLobbyReady();
            return;
        }

        if (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            Debug.LogWarning("서버 연결 상태가 아님!");
            return;
        }

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장! (콜백)");
        lobbyReady = true;
        OnLobbyReady();
    }

    private async void OnLobbyReady()
    {
        if (quickMachingbtn) quickMachingbtn.interactable = true;

        if (db != null)
            await db.LoaduserDataAsync();
    }

    public void RandomOrCreateRoom()
    {
        Debug.Log($"빠른 입장 버튼 클릭했음! 현재 로비인가?: {PhotonNetwork.InLobby}, 클라이언트 상태: {PhotonNetwork.NetworkClientState}");

        if (!lobbyReady)
        {
            Debug.LogWarning("아직 준비가 안되어있는데?");
            return;
        }

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"아무 방 들어가기 실패! {returnCode} {message}");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"방에 입장! (콜백) {PhotonNetwork.CurrentRoom.Name}");

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("RoomScene");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"연결 끊김! (콜백) {cause}, 끊기기 전 클라이언트 상태: {PhotonNetwork.NetworkClientState}");
        if (quickMachingbtn) quickMachingbtn.interactable = false;
        lobbyReady = false;
    }
}
