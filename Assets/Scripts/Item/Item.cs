using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [SerializeField, ReadOnly] private string id;    // 고유 ID
    public string ID => id;
    [SerializeField] private string itemName;
    public string ItemName => itemName;
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

        itemName = this.name;
    }
#endif
}

[System.Serializable]
public struct HintData
{
    public string hintKey;   // 예: "WIRE_COLOR_MAP"
    public string payload;   // 예: "12345" (seed)

    public bool HasValue => !string.IsNullOrEmpty(hintKey);
    public static HintData Empty => new HintData { hintKey = null, payload = null };
}

[System.Serializable]
public class ItemInstance
{
    public string itemId;   // Item SO GUID
    public HintData hint;   // 동적 힌트 데이터

    public ItemInstance(string itemId, HintData hint)
    {
        this.itemId = itemId;
        this.hint = hint;
    }
}