using UnityEngine;
using UnityEngine.UI;

public enum SlotType
{
    Inventory,
    Quick
}

// 들어온 정보, 초기화만 해줌.
public class Slot : MonoBehaviour
{
    public Item currentItem;
    public Image iconImage;
    public Image backgroundImage;
    public SlotType slotType;
    public int slotIndex;

    public void AddItem(Item newItem)
    {
        currentItem = newItem;
        iconImage.sprite = newItem.itemIcon;
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
    }

    public bool IsEmptySlot() => currentItem == null;
}
