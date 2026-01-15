using UnityEngine;
using UnityEngine.InputSystem;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance;
    const int MAX_SLOT_COUNT = 4;

    [Header("Slot")]
    [SerializeField] private Transform slotPrefab;   // 슬롯 프리팹
    public Slot[] slots;
    private int forcusedIndex;

    private void Awake()
    {
        Instance = this;
        slots = new Slot[MAX_SLOT_COUNT];

        if (slotPrefab != null)
        {
            GameObject quickSlotObj = GameObject.FindGameObjectWithTag("QuickSlot");

            for (int i = 0; i < MAX_SLOT_COUNT; i++)
            {
                GameObject newSlotObj = Instantiate(slotPrefab.gameObject, quickSlotObj.transform.GetChild(0));
                slots[i] = newSlotObj.GetComponent<Slot>();

                slots[i].slotType = SlotType.Quick;
                slots[i].slotIndex = i;
            }
        }
    }

    // 아이템 추가 (아이템)
    public void AddItem(Item item)
    {
        foreach (Slot slot in slots)
        {
            if (slot.IsEmptySlot())
            {
                slot.AddItem(item);
                break;
            }
        }
    }

    // 아이템 제거 (인덱스)
    public void RemoveItem()
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return;
        if (slots[forcusedIndex].IsEmptySlot()) return;

        slots[forcusedIndex].ClearSlot();
    }

    public bool CompareItem(string itemID)
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return false;
        if (slots[forcusedIndex].IsEmptySlot()) return false;

        if (slots[forcusedIndex].currentItem.ID.Equals(itemID))
            return true;
        else
        {
            Debug.Log("다른 아이템입니다!");
            return false;
        }
    }

    // 포커싱된 슬롯 색 변경
    public void UpdateSlotFocusedColor(int index)
    {
        forcusedIndex = index;

        for (int i = 0; i < MAX_SLOT_COUNT; i++)
        {
            if (i == index)
                slots[i].backgroundImage.color = Color.green;
            else
                slots[i].backgroundImage.color = Color.black;
        }
    }

    public void UpdateSlotData(int index, string itemID)
    {
        Item newItem = ItemManager.Instance.GetItemById(itemID);

        slots[index].ClearSlot();

        if (newItem != null) 
            slots[index].AddItem(newItem);
    }

    // 매개변수로 받은 인덱스에 존재하는 아이템 ID를 반환
    public string GetItemIdByIndex(int index)
    {
        if (slots[index].IsEmptySlot()) return null;
        return slots[index].currentItem.ID;
    }
    public int GetMaxSlotCount() => MAX_SLOT_COUNT;
}
