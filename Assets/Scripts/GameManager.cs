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
    public Dictionary<int, GameObject> playerData;  // <ActorNumber, 플레이어 옵젝>
    [SerializeField] private Transform playerPrefab;
    
    void Awake()
    {
        Instance = this;
        playerData = new Dictionary<int, GameObject>();
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
        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, randPos, Quaternion.identity);
        PhotonView playerPv = newPlayer.GetComponent<PhotonView>();

        // 플레이어 데이터 저장
        AddPlayer(playerPv.Owner.ActorNumber, newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int actorNum = otherPlayer.ActorNumber;
        if (playerData.ContainsKey(actorNum))
        {
            PhotonNetwork.Destroy(playerData[actorNum]);
            RemovePlayer(actorNum);
        }
    }

    private void AddPlayer(int actorNumber, GameObject playerObj)
    {
        playerData[actorNumber] = playerObj;
    }

    private void RemovePlayer(int actorNumber)
    {
        playerData.Remove(actorNumber);
    }
}
