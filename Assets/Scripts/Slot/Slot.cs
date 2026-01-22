using UnityEngine;
using UnityEngine.UI;

public enum SlotType
{
    Inventory,
    Quick
}

[System.Serializable]
public struct HintData
{
    public string hintKey;   // 예: "WIRE_COLOR_MAP"
    public string payload;   // 예: "12345" (seed)

    public bool HasValue => !string.IsNullOrEmpty(hintKey);
    public static HintData Empty => new HintData { hintKey = null, payload = null };
}

// 들어온 정보, 초기화만 해줌.
public class Slot : MonoBehaviour
{
    public Item currentItem;
    public HintData currentHint;
    public Image iconImage;
    public Image backgroundImage;
    public SlotType slotType;
    public int slotIndex;

    public void AddItem(Item newItem)
    {
        currentItem = newItem;
        iconImage.sprite = newItem.itemIcon;
    }

    public void AddHintItem(Item paperItem, string hintKey, string payload)
    {
        currentItem = paperItem;
        iconImage.sprite = paperItem.itemIcon;
        currentHint = new HintData { hintKey = hintKey, payload = payload };
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
        currentHint = HintData.Empty;
    }

    public bool IsEmptySlot() => currentItem == null;
}
