using UnityEngine;
using System.Linq;

public class SpawnPointGroup : MonoBehaviour
{
    [SerializeField] private Transform parent;
    public Transform[] Points { get; private set; }

    private void Awake()
    {
        if (parent == null) parent = this.transform;

        Points = parent.GetComponentsInChildren<Transform>()
                    .Where(t => t != parent)
                    .ToArray();
 
    }

    public Transform Get(int index) => Points[index];
    public int Count => Points.Length;
}
