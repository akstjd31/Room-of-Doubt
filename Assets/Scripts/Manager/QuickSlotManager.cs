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
    [SerializeField] private int forcusedIndex;

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

        forcusedIndex = -1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            ReadFocusedHint();
    }

    // 아이템 추가 (아이템)
    public void AddItem(ItemInstance item)
    {
        foreach (Slot slot in slots)
        {
            if (slot.IsEmptySlot())
            {
                slot.Set(item);
                break;
            }
        }
    }

    public void SetHintToSlot(int slotIndex, Item paperItem, string hintKey, string payload)
    {
        var inst = new ItemInstance(paperItem.ID, new HintData { hintKey = hintKey, payload = payload });
        slots[slotIndex].Set(inst);
    }

    public string ReadFocusedHint()
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return null;

        var slot = slots[forcusedIndex];
        if (slot.IsEmptySlot()) return null;
        if (!slot.current.hint.HasValue)
        {
            Debug.Log("이 슬롯에는 힌트 데이터가 없음");
            return null;
        }

        // HintDatabase: hintId + payload로 실제 문장 렌더링하는 쪽
        string text = HintDatabase.Instance.Render(slot.current.hint.hintKey, slot.current.hint.payload);
        return text;
    }

    // 아이템 제거 (인덱스)
    public void RemoveItem()
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return;
        if (slots[forcusedIndex].IsEmptySlot()) return;

        slots[forcusedIndex].Clear();
    }

    public ItemInstance GetItemInstanceByIndex(int index)
    {
        if (index < 0 || index >= MAX_SLOT_COUNT) return null;
        return slots[index].current;
    }

    public bool CompareItem(string itemID)
    {
        if (forcusedIndex < 0 || forcusedIndex >= MAX_SLOT_COUNT) return false;
        if (slots[forcusedIndex].IsEmptySlot()) return false;

        if (slots[forcusedIndex].current.itemId.Equals(itemID))
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

    public void UpdateSlotData(int index, ItemInstance inst)
    {
        if (index < 0 || index >= MAX_SLOT_COUNT) return;

        slots[index].Clear();
        if (inst == null) return;

        if (inst != null)
            slots[index].Set(inst);
    }

    // 매개변수로 받은 인덱스에 존재하는 아이템 ID를 반환
    public string GetItemIdByIndex(int index)
    {
        if (slots[index].IsEmptySlot()) return "";
        return slots[index].current.itemId;
    }
    
    // 현재 포커싱 중인 슬롯
    public Slot GetFocusedSlot()
    {
        if (forcusedIndex < 0 || forcusedIndex >= slots.Length) return null;
        return slots[forcusedIndex];
    }
    public int GetMaxSlotCount() => MAX_SLOT_COUNT;
}
