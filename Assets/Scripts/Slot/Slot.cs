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
    public ItemInstance current;
    public Image iconImage;
    public Image backgroundImage;
    public SlotType slotType;
    public int slotIndex;

    public void Set(ItemInstance inst)
    {
        current = inst;
        var so = ItemManager.Instance.GetItemById(inst.itemId);
        iconImage.sprite = so != null ? so.itemIcon : null;
    }

    public void Clear()
    {
        current = null;
        iconImage.sprite = null;
    }

    public bool IsEmptySlot() => current == null;
}
