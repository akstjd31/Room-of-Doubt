using UnityEngine;
using UnityEngine.UI;
public class Slot : MonoBehaviour
{
    public Item currentItem;
    public Image iconImage;

    public void AddItem(Item newItem)
    {
        currentItem = newItem;
        iconImage.sprite = newItem.itemIcon;
        iconImage.enabled = true;
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.enabled = false;
    }
}
