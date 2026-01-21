using UnityEngine;
using UnityEngine.InputSystem;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance;
    const int MAX_SLOT_COUNT = 4;

    [Header("Slot")]
    [SerializeField] private GameObject quickSlotParent;        // 판넬
    [SerializeField] private Transform slotPrefab;              // 슬롯 프리팹
    public Slot[] slots;
    private int forcusedIndex;

    private void Awake()
    {
        Instance = this;
        slots = new Slot[MAX_SLOT_COUNT];

        if (slotPrefab != null)
        {
            for (int i = 0; i < MAX_SLOT_COUNT; i++)
            {
                GameObject newSlotObj = Instantiate(slotPrefab.gameObject, quickSlotParent.transform);
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

    public void SetHintToSlot(int slotIndex, Item paperItem, int hintId, string payload)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOT_COUNT) return;

        slots[slotIndex].ClearSlot();
        slots[slotIndex].AddHintItem(paperItem, hintId, payload);
    }

    public void ReadFocusedHint()
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return;

        var slot = slots[forcusedIndex];
        if (slot.IsEmptySlot()) return;
        if (!slot.currentHint.HasValue)
        {
            Debug.Log("이 슬롯에는 힌트 데이터가 없음");
            return;
        }

        // // HintDatabase: hintId + payload로 실제 문장 렌더링하는 쪽
        // string text = HintDatabase.Instance.Render(slot.currentHint.hintId, slot.currentHint.payload);
        // UIManager.Instance.ShowDocument(text);
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

    public void SetActiveSlotParent(bool active) => quickSlotParent.SetActive(active);

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
        if (slots[index].IsEmptySlot()) return "";
        return slots[index].currentItem.ID;
    }
    public int GetMaxSlotCount() => MAX_SLOT_COUNT;
}
