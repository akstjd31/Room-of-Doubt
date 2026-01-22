using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PhotonPrefabPoolManager : Singleton<PhotonPrefabPoolManager>, IPunPrefabPool
{
    [Header("Optional Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private int cachingCountPerPrefab = 0;

    private readonly Dictionary<string, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, GameObject> prefabCache = new();

    private void Awake()
    {
        PhotonNetwork.PrefabPool = this;
    }

    public void Preload(string prefabPath)
    {
        GetOrCachePrefab(prefabPath);

        if (cachingCountPerPrefab <= 0) return;

        if (!pool.TryGetValue(prefabPath, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefabPath] = q;
        }

        while (q.Count < cachingCountPerPrefab)
        {
            var prefab = prefabCache[prefabPath];
            var obj = Instantiate(prefab, poolParent);
            obj.name = prefab.name;
            obj.SetActive(false);
            q.Enqueue(obj);
        }
    }

public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
{
    var prefab = GetOrCachePrefab(prefabId);
    if (prefab == null) return null;

    // ✅ 풀링 제외(플레이어 포함 제외 대상): 그냥 일반 생성으로 반환
    if (!ShouldPool(prefabId))
    {
        var obj = Object.Instantiate(prefab, position, rotation);
        obj.name = prefab.name;

        // Photon 경고 방지: PrefabPool은 inactive를 리턴하는 게 정석
        if (obj.activeSelf) obj.SetActive(false);

        // Destroy에서 ShouldPool 판단할 때 필요하니 기록은 해두자(선택)
        var tag = obj.GetComponent<PhotonPoolTag>() ?? obj.AddComponent<PhotonPoolTag>();
        tag.PrefabId = prefabId;

        return obj;
    }

    // ✅ 여기부터 풀링 대상(Items/Puzzles)
    if (!pool.TryGetValue(prefabId, out var q))
        pool[prefabId] = q = new Queue<GameObject>();

    GameObject pooled = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, poolParent);
    pooled.name = prefab.name;

    if (pooled.activeSelf) pooled.SetActive(false);

    var pooledTag = pooled.GetComponent<PhotonPoolTag>() ?? pooled.AddComponent<PhotonPoolTag>();
    pooledTag.PrefabId = prefabId;

    pooled.transform.SetPositionAndRotation(position, rotation);
    return pooled;
}

public void Destroy(GameObject gameObject)
{
    if (gameObject == null) return;

    var tag = gameObject.GetComponent<PhotonPoolTag>();

    // ✅ 태그가 없거나, 풀링 제외 대상이면 "진짜 파괴"
    if (tag == null || string.IsNullOrEmpty(tag.PrefabId) || !ShouldPool(tag.PrefabId))
    {
        Object.Destroy(gameObject);   // ✅ DestroyImmediate 말고 Destroy 추천
        return;
    }

    // ✅ 풀링 대상만 반환
    PhotonPoolUtil.ResetAllPhotonViewIds(gameObject);
    gameObject.SetActive(false);
    gameObject.transform.SetParent(poolParent);

    if (!pool.TryGetValue(tag.PrefabId, out var q))
        pool[tag.PrefabId] = q = new Queue<GameObject>();

    q.Enqueue(gameObject);
}

    private bool ShouldPool(string prefabId)
    {
        // 풀링 허용: Items, Puzzles만
        return prefabId.StartsWith("Items/") || prefabId.StartsWith("Puzzles/");
    }


    private GameObject GetOrCachePrefab(string prefabPath)
    {
        if (!prefabCache.TryGetValue(prefabPath, out var prefab) || prefab == null)
        {
            prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"로드 실패 : {prefabPath}");
                return null;
            }

            prefabCache[prefabPath] = prefab;
        }

        return prefab;
    }
}

// 어떤 경로로부터 왔는지 저장하기 위한 컴포넌트
public class PhotonPoolTag : MonoBehaviour
{
    public string PrefabId;
}

static class PhotonPoolUtil
{
    public static void ResetAllPhotonViewIds(GameObject go)
    {
        var views = go.GetComponentsInChildren<PhotonView>(true);
        foreach (var v in views)
            v.ViewID = 0; // 중요: 재사용 전에 반드시 초기화
    }
}