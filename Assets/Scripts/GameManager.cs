using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public const float MAX_X = 1f;
    public const float MIN_X = -1f;
    public const float MAX_Y = 5f;
    public const float MIN_Y = 2f;
    public static GameManager Instance; 
    // public Dictionary<int, GameObject> playerDict;
    [SerializeField] private Transform playerPrefab;
    
    void Awake()
    {
        Instance = this;
        // playerDict = new Dictionary<int, GameObject>();
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
        PhotonNetwork.Instantiate(playerPrefab.name, randPos, Quaternion.identity);
    }
}
