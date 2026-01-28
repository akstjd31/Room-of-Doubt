using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private FirebaseDBManager db;
    IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnected);
        PhotonNetwork.JoinLobby();
        
        DataLoad();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비에 입장하셨습니다.");
    }

    // 버튼 이벤트 (빠참)
    public void RandomOrCreateRoom()
    {
        if (!PhotonNetwork.InLobby)
            return;
            
        SceneManager.LoadScene("RoomScene");
    }

    public async void DataLoad()
    {
        await db.LoaduserDataAsync();
    }

    public async void DataSave()
    {
        await db.SaveUserDataAsync();
    }
}
