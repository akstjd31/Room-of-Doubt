using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class SpawnManager : MonoBehaviourPunCallbacks
{
    public static SpawnManager Instance;
    private const string KEY_SEED = "SPAWN_SEED";   // 룸에 저장될 시드
    private const string KEY_DONE = "SPAWN_DONE";   // 중복 스폰 방지용

    [SerializeField] private SpawnPointGroup itemSpawnPoints;
    [SerializeField] private SpawnPointGroup puzzleSpawnPoints;
    [SerializeField] private string itemResourcesFolder = "Items";
    [SerializeField] private string puzzleResourcesFolder = "Puzzles";
    [SerializeField] private List<string> itemPrefabPaths;
    [SerializeField] private List<string> puzzlePrefabPaths;


    public bool SpawnedLocally { get; private set; }

    private void Awake()
    {
        Instance = this;

        LoadPrefabsFromResources(itemPrefabPaths, itemResourcesFolder);
        LoadPrefabsFromResources(puzzlePrefabPaths, puzzleResourcesFolder);

        foreach (var p in itemPrefabPaths) PhotonPrefabPoolManager.Instance.Preload(p);
        foreach (var p in puzzlePrefabPaths) PhotonPrefabPoolManager.Instance.Preload(p);
    }

    // 해당 경로에 존재하는 아이템 경로 따오기
    private void LoadPrefabsFromResources(List<string> prefabPaths, string resourceFolder)
    {
        prefabPaths.Clear();
        var loadedPrefabs = Resources.LoadAll<GameObject>(resourceFolder);

        foreach (var prefab in loadedPrefabs)
        {
            if (prefab.name.Equals("Lamp")) continue;

            string path = $"{resourceFolder}/{prefab.name}";
            prefabPaths.Add(path);
        }

        Debug.Log($"{resourceFolder} 경로 매핑 성공!");
    }

    private void Start()
    {
        StartCoroutine(WaitAndInit());
    }

    private IEnumerator WaitAndInit()
    {
        // yield return new WaitForSeconds(1f);
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
        Debug.Log("시드 생성 완료!");
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
        if (SpawnedLocally) return;

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
            SpawnedLocally = true;
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;

        if (itemSpawnPoints == null || itemSpawnPoints.Count < 1) return;
        if (itemPrefabPaths == null || itemPrefabPaths.Count < 1) return;
        if (puzzleSpawnPoints == null || puzzleSpawnPoints.Count < 1) return;
        if (puzzlePrefabPaths == null || puzzlePrefabPaths.Count < 1) return;

        var rand = new System.Random(seed);

        int spawnCount = Mathf.Min(itemPrefabPaths.Count, itemSpawnPoints.Count);

        var itemIndices = new List<int>(itemSpawnPoints.Count);
        for (int i = 0; i < itemSpawnPoints.Count; i++)
            itemIndices.Add(i);

        // 전체 인덱스 셔플
        Shuffle(itemIndices, rand);
        
        for (int i = 0; i < spawnCount; i++)
        {
            string path = itemPrefabPaths[i];
            int spawnIndex = itemIndices[i];
            var t = itemSpawnPoints.Get(spawnIndex);

            var obj = PhotonNetwork.InstantiateRoomObject(path, t.position, t.rotation);
            if (obj == null) Debug.LogError($"프리팹 로드 실패: {path}");
        }

        int puzzleCount = Mathf.Min(puzzlePrefabPaths.Count, puzzleSpawnPoints.Count);

        var puzzleIndices = new List<int>(puzzleSpawnPoints.Count);
        for (int i = 0; i < puzzleSpawnPoints.Count; i++)
            puzzleIndices.Add(i);

        Shuffle(puzzleIndices, rand);

        for (int i = 0; i < puzzleCount; i++)
        {
            string path = puzzlePrefabPaths[i];
            int spawnIndex = puzzleIndices[i];
            var t = puzzleSpawnPoints.Get(spawnIndex);

            var obj = PhotonNetwork.InstantiateRoomObject(path, t.position, t.rotation);
            if (obj == null) Debug.LogError($"프리팹 로드 실패: {path}");
        }


        room.SetCustomProperties(new PhotonHashtable { { KEY_DONE, true } });
        SpawnedLocally = true;

        Debug.Log("모든 아이템, 퍼즐 생성 완료!");
    }

    // 셔플
    private void Shuffle<T>(List<T> list, System.Random rand)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
