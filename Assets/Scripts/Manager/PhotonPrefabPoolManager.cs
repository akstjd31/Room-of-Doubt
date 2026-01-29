using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Photon 네트워크 Instantiate/Destroy 경로는 netPool로,
/// 로컬 Inspect/미리보기(GetLocal/ReleaseLocal)는 localPool로 분리한다.
/// </summary>
public class PhotonPrefabPoolManager : MonoBehaviourPun, IPunPrefabPool
{
    public static PhotonPrefabPoolManager Instance;

    [Header("Optional Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private int cachingCountPerPrefab = 0;

    // ✅ 풀 분리 (중요)
    private readonly Dictionary<string, Queue<GameObject>> netPool = new();
    private readonly Dictionary<string, Queue<GameObject>> localPool = new();

    private readonly Dictionary<string, GameObject> prefabCache = new();

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.PrefabPool = this;
    }

    /// <summary>
    /// 캐싱: 네트워크/로컬 둘 다 쓸 수 있도록 localPool에 미리 쌓아두는 방식.
    /// (원하면 net/local 각각 따로 캐싱하도록 분리해도 됨)
    /// </summary>
    public void Preload(string prefabPath)
    {
        GetOrCachePrefab(prefabPath);

        if (cachingCountPerPrefab <= 0) return;

        if (!localPool.TryGetValue(prefabPath, out var q))
            localPool[prefabPath] = q = new Queue<GameObject>();

        while (q.Count < cachingCountPerPrefab)
        {
            var prefab = prefabCache[prefabPath];
            var obj = Instantiate(prefab, poolParent);
            PreparePooledObject(obj, prefabPath, isNetwork: false);
            q.Enqueue(obj);
        }
    }

    // =========================
    // Photon 네트워크 풀 경로
    // =========================

    /// <summary>
    /// PhotonNetwork.Instantiate/InstantiateRoomObject 시 호출된다.
    /// </summary>
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        var prefab = GetOrCachePrefab(prefabId);
        if (prefab == null) return null;

        // 풀링 대상이 아니면 그냥 생성해서 반환(Photon이 viewID 할당)
        if (!ShouldPool(prefabId))
        {
            var obj = Object.Instantiate(prefab, position, rotation);
            obj.name = prefab.name;
            obj.SetActive(false);

            var tag = obj.GetComponent<PhotonPoolTag>() ?? obj.AddComponent<PhotonPoolTag>();
            tag.PrefabId = prefabId;

            return obj;
        }

        if (!netPool.TryGetValue(prefabId, out var q))
            netPool[prefabId] = q = new Queue<GameObject>();

        GameObject pooled = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, poolParent);
        pooled.name = prefab.name;

        // Photon이 재할당할 거라, 여기서 ViewID를 건드리지 말고 비활성 유지
        pooled.SetActive(false);

        var pooledTag = pooled.GetComponent<PhotonPoolTag>() ?? pooled.AddComponent<PhotonPoolTag>();
        pooledTag.PrefabId = prefabId;

        pooled.transform.SetPositionAndRotation(position, rotation);
        return pooled;
    }

    /// <summary>
    /// PhotonNetwork.Destroy 시 호출된다.
    /// </summary>
    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;

        var pv = gameObject.GetComponent<PhotonView>();

        // ✅ SceneView(씬 오브젝트)는 절대 여기서 풀링/조작하지 말고 그냥 파괴
        if (pv != null && pv.IsSceneView)
        {
            Object.Destroy(gameObject);
            return;
        }

        var tag = gameObject.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId) || !ShouldPool(tag.PrefabId))
        {
            Object.Destroy(gameObject);
            return;
        }

        // ✅ 권한 있을 때만 RemoveRPCs (아니면 에러)
        if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
            PhotonNetwork.RemoveRPCs(pv);

        // ✅ 네트워크 오브젝트는 재사용을 위해 ViewID 초기화
        ResetAllPhotonViewIds(gameObject);

        gameObject.SetActive(false);
        gameObject.transform.SetParent(poolParent, false);

        if (!netPool.TryGetValue(tag.PrefabId, out var q))
            netPool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(gameObject);
    }

    // =========================
    // 로컬 풀 경로 (Inspect 등)
    // =========================

    public GameObject GetLocal(string prefabId, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        var prefab = GetOrCachePrefab(prefabId);
        if (prefab == null) return null;

        if (!localPool.TryGetValue(prefabId, out var q))
            localPool[prefabId] = q = new Queue<GameObject>();

        GameObject obj = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, poolParent);
        PreparePooledObject(obj, prefabId, isNetwork: false);

        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;

        obj.SetActive(true);
        return obj;
    }

    public void ReleaseLocal(GameObject obj)
    {
        if (obj == null) return;

        var tag = obj.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId))
        {
            Object.Destroy(obj);
            return;
        }

        // ✅ 로컬 풀에는 네트워크 오브젝트가 들어오면 안 됨 (방어)
        var pv = obj.GetComponent<PhotonView>();
        if (pv != null && pv.ViewID != 0)
        {
            Debug.LogWarning($"[ReleaseLocal] network object detected. Use PhotonNetwork.Destroy instead. GO={obj.name} viewId={pv.ViewID}");
            return;
        }

        // 로컬 풀 오브젝트는 ViewID=0 유지(있다면)
        ResetAllPhotonViewIds(obj);

        obj.SetActive(false);
        obj.transform.SetParent(poolParent, false);

        if (!localPool.TryGetValue(tag.PrefabId, out var q))
            localPool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(obj);
    }

    // =========================
    // 내부 유틸
    // =========================

    private void PreparePooledObject(GameObject obj, string prefabId, bool isNetwork)
    {
        obj.name = obj.name.Replace("(Clone)", "").Trim();

        var tag = obj.GetComponent<PhotonPoolTag>() ?? obj.AddComponent<PhotonPoolTag>();
        tag.PrefabId = prefabId;

        // 로컬 풀로 쓰는 오브젝트는 항상 ViewID 0 상태로 유지하는 게 안전
        if (!isNetwork)
            ResetAllPhotonViewIds(obj);

        obj.SetActive(false);
        obj.transform.SetParent(poolParent, false);
    }

    private bool ShouldPool(string prefabId)
    {
        return prefabId.StartsWith("Items/") ||
               prefabId.StartsWith("Puzzles/") ||
               prefabId.StartsWith("Hints/");
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

    private static void ResetAllPhotonViewIds(GameObject go)
    {
        var views = go.GetComponentsInChildren<PhotonView>(true);
        foreach (var v in views)
            v.ViewID = 0;
    }
}

/// <summary>
/// 어떤 경로로부터 왔는지 저장하기 위한 컴포넌트
/// </summary>
public class PhotonPoolTag : MonoBehaviour
{
    public string PrefabId;
}
