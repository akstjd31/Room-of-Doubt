using UnityEngine;

// 아이템 전체 데이터 중 검색 가능
public class ItemManager : Singleton<ItemManager>
{
    [SerializeField] private ItemDatabase itemSO;

    // ID로 아이템 검색
    public Item GetItemById(string id)
    {
        if (itemSO == null) return null;

        foreach (Item item in itemSO.items)
        {
            if (item.ID.Equals(id))
                return item;
            
        }

        return null;
    }

    public Item GetItemByType(ItemKind kind)
    {
        if (itemSO == null) return null;

        foreach (Item item in itemSO.items)
        {
            if (item.Kind.Equals(kind))
                return item;
        }

        return null;
    }
}
