using UnityEngine;
using System;

public enum ItemKind
{
    Normal,
    HintPaper,
    Lamp,
}

// 사용하면 없어지는가? / 없어지지 않는가?
public enum ConsumeType
{
    Consumable, 
    Permanent
}
[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    [SerializeField, ReadOnly] private string id;
    public string ID => id;

    [SerializeField] private string itemName;
    public string ItemName => itemName;

    public Sprite itemIcon;
    public Transform itemPrefab;

    [Header("Type")]
    [SerializeField] private ItemKind kind = ItemKind.Normal;
    public ItemKind Kind => kind;

    [SerializeField] private ConsumeType consumeType = ConsumeType.Consumable;
    public ConsumeType ConsumeType => consumeType;
    public bool IsLamp => kind == ItemKind.Lamp;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
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
    public string requiredToUseId;
    public bool hasRequiredPart;
    public HintData hint;   // 동적 힌트 데이터

    public ItemInstance(string itemId, HintData hint, string requiredToUseId = null)
    {
        this.itemId = itemId;
        this.hint = hint;
        this.requiredToUseId = requiredToUseId;
    }
}