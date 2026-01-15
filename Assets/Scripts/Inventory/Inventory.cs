using System.Collections.Generic;
using UnityEngine;

// 현재 보관하고 있는 아이템 관리
// 아이템이 추가, 제거, 탐색 가능한 기능
public class Inventory : MonoBehaviour
{
    public List<Item> inventoryItems;
    private void Awake()
    {
        inventoryItems = new List<Item>();
    }

    public void AddItem(Item item)
    {
        inventoryItems.Add(item);
    }

    public void RemoveItem(Item item)
    {
        inventoryItems.Remove(item);
    }

    // 특정 아이템을 받아 현재 있는 리스트에 존재하는지 여부 판단
    public Item FindItem(Item item)
    {
        foreach (Item invenItem in inventoryItems)
        {
            if (invenItem.Equals(item))
                return invenItem;
        }

        return null;
    }
}
