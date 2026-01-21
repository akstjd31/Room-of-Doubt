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
    public int hintId;
    public string payload;
    public bool HasValue => hintId != 0 || !string.IsNullOrEmpty(payload);
    public static HintData Empty => new HintData { hintId = 0, payload = null };
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

    public void AddHintItem(Item paperItem, int hintId, string payload)
    {
        currentItem = paperItem;
        iconImage.sprite = paperItem.itemIcon;
        currentHint = new HintData { hintId = hintId, payload = payload };
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
        currentHint = HintData.Empty;
    }

    public bool IsEmptySlot() => currentItem == null;
}
