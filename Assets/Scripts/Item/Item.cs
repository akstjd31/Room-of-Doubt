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

// Item.cs 안에 추가
[Header("Required Part (Optional)")]
[SerializeField] private Item requiredPart;
public Item RequiredPart => requiredPart;
public string RequiredPartId => requiredPart != null ? requiredPart.ID : null;
public bool RequiresPart => requiredPart != null;


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
    public string itemId;            // 본체 아이템 ID
    public HintData hint;

    // 장착된 부품 (없으면 null)
    public string installedPartId;   // 예: 배터리 ID

    public ItemInstance(string itemId, HintData hint, string installedPartId = null)
    {
        this.itemId = itemId;
        this.hint = hint;
        this.installedPartId = installedPartId;
    }

    public bool HasInstalledPart => !string.IsNullOrEmpty(installedPartId);
}
