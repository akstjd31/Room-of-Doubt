using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 포톤 네트워크로 생성/파괴될 때 오브젝트 풀로 관리하기 위한 클래스
/// </summary>
public class PhotonPrefabPoolManager : MonoBehaviourPun, IPunPrefabPool
{
    public static PhotonPrefabPoolManager Instance;
    [Header("Optional Settings")]
    [SerializeField] private Transform poolParent;                      // 풀 부모
    [SerializeField] private int cachingCountPerPrefab = 0;             // 미리 캐싱된 프리팹 개수

    private readonly Dictionary<string, Queue<GameObject>> pool = new();
    private readonly Dictionary<string, GameObject> prefabCache = new();

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.PrefabPool = this;
    }

    // 미리 캐싱해놓기
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

    // 생성 (포톤 네트워크 생성하면 이게 호출됨)
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        var prefab = GetOrCachePrefab(prefabId);
        if (prefab == null) return null;

        if (!ShouldPool(prefabId))
        {
            var obj = Object.Instantiate(prefab, position, rotation);
            obj.name = prefab.name;

            if (obj.activeSelf) obj.SetActive(false);

            var tag = obj.GetComponent<PhotonPoolTag>() ?? obj.AddComponent<PhotonPoolTag>();
            tag.PrefabId = prefabId;

            return obj;
        }

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

    // 파괴 (포톤 네트워크 파괴하면 이게 호출됨)
    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;

        var tag = gameObject.GetComponent<PhotonPoolTag>();

        if (tag == null || string.IsNullOrEmpty(tag.PrefabId) || !ShouldPool(tag.PrefabId))
        {
            Object.Destroy(gameObject);
            return;
        }

        PhotonPoolUtil.ResetAllPhotonViewIds(gameObject);
        gameObject.SetActive(false);
        gameObject.transform.SetParent(poolParent);

        if (!pool.TryGetValue(tag.PrefabId, out var q))
            pool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(gameObject);
    }

    // 특정 경로만 풀 생성
    private bool ShouldPool(string prefabId)
    {
        return prefabId.StartsWith("Items/") || prefabId.StartsWith("Puzzles/") || prefabId.StartsWith("Hints/");
    }

    // 풀 꺼내기
    public GameObject GetLocal(string prefabId, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        var prefab = GetOrCachePrefab(prefabId);
        if (prefab == null) return null;

        if (!pool.TryGetValue(prefabId, out var q))
            pool[prefabId] = q = new Queue<GameObject>();

        GameObject obj = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, poolParent);
        obj.name = prefab.name;

        // 태그 기록(반납할 때 필요)
        var tag = obj.GetComponent<PhotonPoolTag>() ?? obj.AddComponent<PhotonPoolTag>();
        tag.PrefabId = prefabId;

        // 로컬 배치
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;

        obj.SetActive(true);
        return obj;
    }

    // 회수
    public void ReleaseLocal(GameObject obj)
    {
        if (obj == null) return;

        var tag = obj.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId))
        {
            Object.Destroy(obj);
            return;
        }

        // 네트워크 오브젝트면 여기로 오면 안 되지만, 혹시 모르니 ViewID 리셋은 생략/유지 선택
        // PhotonPoolUtil.ResetAllPhotonViewIds(obj);

        obj.SetActive(false);
        obj.transform.SetParent(poolParent);

        if (!pool.TryGetValue(tag.PrefabId, out var q))
            pool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(obj);
    }

    // 해당 경로로 로드 후 캐시 리스트에 담기
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
            v.ViewID = 0; // 뷰 ID 초기화
    }
}