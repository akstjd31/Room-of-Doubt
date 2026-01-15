using UnityEngine;
using System;


[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [SerializeField, ReadOnly] private string id;    // 고유 ID
    public string ID => id;
    public string itemName;
    public Sprite itemIcon;
    public GameObject itemPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
