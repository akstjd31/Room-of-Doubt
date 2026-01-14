using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public const float MAX_X = 1f;
    public const float MIN_X = -1f;
    public const float MAX_Y = 5f;
    public const float MIN_Y = 2f;
    public static GameManager Instance; 
    public Dictionary<int, QuickSlotManager> playerQuickSlotMgrData;  // <ActorNumber, 플레이어 옵젝>
    [SerializeField] private Transform playerPrefab;
    
    void Awake()
    {
        Instance = this;
        playerQuickSlotData = new Dictionary<int, QuickSlotManager>();
    }

    void Start()
    {
        StartCoroutine(SpawnPlayerWhenConnected());
    }

    IEnumerator SpawnPlayerWhenConnected()
    {
        Vector3 randPos = new Vector3
        (
            Random.Range(MIN_X, MAX_X),
            Random.Range(MIN_Y, MAX_Y),
            Random.Range(MIN_X, MAX_X)
        );

        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        var newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, randPos, Quaternion.identity);
        var playerPv = newPlayer.GetComponent<PhotonView>();

        // 플레이어 데이터 저장
        AddData(playerPv.Owner.ActorNumber, newPlayer.GetComponent<QuickSlotManager>());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int actorNum = otherPlayer.ActorNumber;
        if (playerQuickSlotData.ContainsKey(actorNum))
        {
            PhotonNetwork.Destroy(playerQuickSlotData[actorNum].gameObject);
            RemoveData(actorNum);
        }
    }

    private void AddData(int actorNumber, QuickSlotManager quickSlot)
    {
        playerQuickSlotData[actorNumber] = quickSlot;
    }

    private void RemoveData(int actorNumber)
    {
        playerQuickSlotData.Remove(actorNumber);
    }
}
