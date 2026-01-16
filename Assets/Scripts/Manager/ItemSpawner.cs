using UnityEngine;
using System.Linq;

public class ItemSpawner : Singleton<ItemSpawner>
{
    [SerializeField] private GameObject spawnPointParent;
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        spawnPoints = spawnPointParent
            .GetComponentsInChildren<Transform>()
            .Where(t => t != spawnPointParent.transform)
            .ToArray();
    }
}
