using UnityEngine;
using Photon.Pun;
using System.Collections;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance; 
    [SerializeField] private Transform playerPrefab;
    [SerializeField] private Vector3 spawnPos;
    
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(SpawnPlayerWhenConnected());
    }

    IEnumerator SpawnPlayerWhenConnected()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity);
    }
}
