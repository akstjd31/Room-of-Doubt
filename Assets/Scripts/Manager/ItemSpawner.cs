using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class ItemSpawner : MonoBehaviourPunCallbacks
{
    private const string KEY_SEED = "SPAWN_SEED";   // 룸에 저장될 시드
    private const string KEY_DONE = "SPAWN_DONE";   // 중복 스폰 방지용

    [SerializeField] private SpawnPointGroup spawnPoints;

    [SerializeField] private List<GameObject> itemPrefabs;

    private bool spawnedLocally;
    private void Start()
    {
        StartCoroutine(WaitAndInit());
    }

    private IEnumerator WaitAndInit()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        TryEnsureRoomSpawnSeed();
        TrySpawnFromRoomProps();
    }

    private void TryEnsureRoomSpawnSeed()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 이미 해당 시드가 존재하는 경우
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_SEED)) return;

        // 시드 생성
        int seed = Random.Range(int.MinValue, int.MaxValue);
        var props = new PhotonHashtable
        {
            { KEY_SEED, seed },
            { KEY_DONE, false }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 커스텀 프로퍼티 콜백
    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(KEY_SEED) || propertiesThatChanged.ContainsKey(KEY_DONE))
            TrySpawnFromRoomProps();
    }

    // 시드 배정 및 생성 시도
    private void TrySpawnFromRoomProps()
    {
        if (spawnedLocally) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        // 시드가 없는 경우
        if (!room.CustomProperties.TryGetValue(KEY_SEED, out var seedObj)) return;
        if (!room.CustomProperties.TryGetValue(KEY_DONE, out var doneObj)) return;
        
        int seed = (int)seedObj;
        bool done = (bool)doneObj;

        // 이미 생성이 되었다면 처리
        if (done)
        {
            spawnedLocally = true;
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;

        if (spawnPoints == null || spawnPoints.Count < 1) return;
        if (itemPrefabs == null || itemPrefabs.Count < 1) return;

        var rng = new System.Random(seed);

        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Count; i++)
            indices.Add(i);
        
        // 중복 없는 뽑기
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        
        // 난수 리스트와 인덱스 매핑 후 생성
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            var prefab = itemPrefabs[i];
            int spawnIndex = indices[i];
            var t = spawnPoints.Get(spawnIndex);

            PhotonNetwork.InstantiateRoomObject(prefab.name, t.position, t.rotation);
        }

        room.SetCustomProperties(new PhotonHashtable { { KEY_DONE, true }});
        spawnedLocally = true;
    }
}
