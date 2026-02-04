using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PhotonPrefabPoolManager : MonoBehaviourPun, IPunPrefabPool
{
    public static PhotonPrefabPoolManager Instance;

    [Header("Optional Settings")]
    [SerializeField] private Transform poolParent;
    [SerializeField] private int cachingCountPerPrefab = 0;

    private readonly Dictionary<string, Queue<GameObject>> netPool = new();     // 네트워크 풀 
    private readonly Dictionary<string, Queue<GameObject>> localPool = new();   // 로컬 전용 풀

    private readonly Dictionary<string, GameObject> prefabCache = new();        // 캐시

    private void Awake()
    {
        Instance = this;
        PhotonNetwork.PrefabPool = this;
    }

    // 캐싱: 네트워크/로컬 둘 다 쓸 수 있도록 localPool에 미리 쌓아두는 방식.
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

    // 생성 메서드 정의
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

        // 해당 프리팹 정보가 없다면 큐 만들기
        if (!netPool.TryGetValue(prefabId, out var q))
            netPool[prefabId] = q = new Queue<GameObject>();

        // 기존 큐에 데이터가 존재하면 꺼내고 아니면 생성
        GameObject pooled = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, poolParent);
        pooled.name = prefab.name;

        pooled.SetActive(false);

        // 해당 태그 컴포넌트 달아주기 (ID 정보)
        var pooledTag = pooled.GetComponent<PhotonPoolTag>() ?? pooled.AddComponent<PhotonPoolTag>();
        pooledTag.PrefabId = prefabId;

        pooled.transform.SetPositionAndRotation(position, rotation);
        return pooled;
    }

    // 파괴 메서드 정의
    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;

        var pv = gameObject.GetComponent<PhotonView>();

        // SceneView(씬 오브젝트)는 그냥 파괴
        if (pv != null && pv.IsSceneView)
        {
            Object.Destroy(gameObject);
            return;
        }

        // 오브젝트에 태그 컴포넌트 존재 여부 확인
        var tag = gameObject.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId) || !ShouldPool(tag.PrefabId))
        {
            Object.Destroy(gameObject);
            return;
        }

        // 모든 RPC를 지움. (충돌 방지)
        if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
            PhotonNetwork.RemoveRPCs(pv);
        
        // 해당 오브젝트 포톤 뷰 리셋
        ResetAllPhotonViewIds(gameObject);

        gameObject.SetActive(false);
        gameObject.transform.SetParent(poolParent, false);

        if (!netPool.TryGetValue(tag.PrefabId, out var q))
            netPool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(gameObject);
    }

    // 로컬 생성
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

    // 로컬로 담기
    public void ReleaseLocal(GameObject obj)
    {
        if (obj == null) return;

        var tag = obj.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId))
        {
            Object.Destroy(obj);
            return;
        }

        // 리셋이 안되어있거나, 포톤 뷰 자체가 없다면?
        var pv = obj.GetComponent<PhotonView>();
        if (pv != null && pv.ViewID != 0) return;

        // 로컬 풀 오브젝트는 ViewID=0 유지(있다면)
        ResetAllPhotonViewIds(obj);

        obj.SetActive(false);
        obj.transform.SetParent(poolParent, false);

        if (!localPool.TryGetValue(tag.PrefabId, out var q))
            localPool[tag.PrefabId] = q = new Queue<GameObject>();

        q.Enqueue(obj);
    }

    // 오브젝트 생성 시 준비단계
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

    // 해당 경로만 풀 가능
    private bool ShouldPool(string prefabId)
    {
        return prefabId.StartsWith("Items/") ||
               prefabId.StartsWith("Puzzles/") ||
               prefabId.StartsWith("GlowHints/");
    }

    // 경로에서 프리팹 정보 가져오기
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

    // ViewID를 0으로 만들어버림
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
