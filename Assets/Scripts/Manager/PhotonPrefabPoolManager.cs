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
        if (!pool.TryGetValue(prefabId, out var q))
        {
            q = new Queue<GameObject>();
            pool[prefabId] = q;
        }

        GameObject obj;

        if (q.Count > 0)
        {
            obj = q.Dequeue();
        }
        else
        {
            var prefab = GetOrCachePrefab(prefabId);
            obj = Instantiate(prefab, poolParent);
            obj.name = prefab.name;
        }

        var tag = obj.GetComponent<PhotonPoolTag>();
        if (tag == null) tag = obj.AddComponent<PhotonPoolTag>();
        tag.PrefabId = prefabId;

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);

        return obj;
    }

    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;

        var tag = gameObject.GetComponent<PhotonPoolTag>();
        if (tag == null || string.IsNullOrEmpty(tag.PrefabId))
        {
            DestroyImmediate(gameObject);
            return;
        }

        gameObject.SetActive(false);
        gameObject.transform.SetParent(poolParent);

        if (!pool.TryGetValue(tag.PrefabId, out var q))
        {
            q = new Queue<GameObject>();
            pool[tag.PrefabId] = q;
        }

        q.Enqueue(gameObject);
    }

    private bool ShouldPool(string prefabId, GameObject prefab)
    {
        if (prefab.CompareTag("Player"))
            return false;

        return true;
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
