using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine;


public class QuickSlotManager : MonoBehaviour
{
    // 이건 인스턴스가 보장된 로컬 퀵 슬롯
    public static QuickSlotManager Local
    {
        get
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.LocalQuickSlot;
        }
    }

    const int MAX_SLOT_COUNT = 4;

    [Header("Slot")]
    [SerializeField] private GameObject quickSlotParent;        // 판넬
    [SerializeField] private Transform slotPrefab;              // 슬롯 프리팹
    private Slot[] slots;
    [SerializeField] private int focusedIndex;

    public int OwnerActorNumber { get; private set; } = -1;

    public bool IsAssigned => OwnerActorNumber != -1;

    public void AssignOwner(int actorNumber)
    {
        OwnerActorNumber = actorNumber;
    }

    public void ClearOwner()
    {
        OwnerActorNumber = -1;
        AllClear();
    }

    private void Awake()
    {
        slots = new Slot[MAX_SLOT_COUNT];

        if (slotPrefab != null)
        {
            for (int i = 0; i < MAX_SLOT_COUNT; i++)
            {
                GameObject newSlotObj = Instantiate(slotPrefab.gameObject, quickSlotParent.transform);
                slots[i] = newSlotObj.GetComponent<Slot>();

                slots[i].Clear();
                slots[i].slotType = SlotType.Quick;
                slots[i].slotIndex = i;
            }
        }

        focusedIndex = -1;
    }

    // 아이템 추가 (아이템)
    public bool AddItem(ItemInstance item)
    {
        foreach (Slot slot in slots)
        {
            if (slot.IsEmptySlot())
            {
                slot.Set(item);
                NotifySnapshotToMaster();
                SaveSnapshotToProps();
                return true;
            }
        }

        NotifySnapshotToMaster();
        SaveSnapshotToProps();
        return false;
    }

    // 네트워크 전송을 위한 문자열 변환 작업
    public string[] PackSnapshotFlat()
    {
        int max = GetMaxSlotCount();
        var flat = new string[max * 3];

        for (int i = 0; i < max; i++)
        {
            var inst = GetItemInstanceByIndex(i);
            int baseIdx = i * 3;

            // 만약 null 이라면 비어있는 문자열 보내기 (null은 오류 발생하기 쉬운 문제)
            if (inst == null)
            {
                flat[baseIdx + 0] = "";
                flat[baseIdx + 1] = "";
                flat[baseIdx + 2] = "";
                continue;
            }

            // 1번째 칸: 아이템 ID
            flat[baseIdx + 0] = inst.itemId ?? "";

            // 2번째 칸: 힌트 키
            // 3번째 칸: 힌트 데이터
            if (inst.hint.HasValue)
            {
                flat[baseIdx + 1] = inst.hint.hintKey ?? "";
                flat[baseIdx + 2] = inst.hint.payload ?? "";
            }
            else
            {
                flat[baseIdx + 1] = "";
                flat[baseIdx + 2] = "";
            }
        }

        return flat;
    }

    // 스냅 샷 형태로 만들어서 마스터한테 전송
    public void NotifySnapshotToMaster()
    {
        if (!PhotonNetwork.InRoom) return;

        var flat = PackSnapshotFlat();

        Debug.Log($"[QS SEND] actor={PhotonNetwork.LocalPlayer.ActorNumber}, len={flat.Length}");
        
        // 네트워크 이벤트 전송
        PhotonNetwork.RaiseEvent(
            QuickSlotNet.EVT_QUICKSLOT_SNAPSHOT,    // 이게 뭔데?
            flat,                                   // 형태는?
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    // 힌트인 경우
    public void SetHintToSlot(int slotIndex, Item paperItem, string hintKey, string payload)
    {
        var inst = new ItemInstance(paperItem.ID, new HintData { hintKey = hintKey, payload = payload });
        slots[slotIndex].Set(inst);

        NotifySnapshotToMaster();
        SaveSnapshotToProps();
    }

    // F키로 힌트 읽기
    public string ReadFocusedHint()
    {
        if (focusedIndex < 0 || focusedIndex >= MAX_SLOT_COUNT) return null;

        var slot = slots[focusedIndex];
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
        if (focusedIndex < 0 || focusedIndex >= MAX_SLOT_COUNT) return;
        if (slots[focusedIndex].IsEmptySlot()) return;

        slots[focusedIndex].Clear();

        NotifySnapshotToMaster();
        SaveSnapshotToProps();
    }

    public ItemInstance GetItemInstanceByIndex(int index)
    {
        if (index < 0 || index >= MAX_SLOT_COUNT) return null;
        return slots[index].current;
    }

    public bool CompareItem(string itemID)
    {
        if (focusedIndex < 0 || focusedIndex >= MAX_SLOT_COUNT) return false;
        if (slots[focusedIndex].IsEmptySlot()) return false;


        if (slots[focusedIndex].current.itemId.Equals(itemID))
            return true;
        else
        {
            Debug.Log("다른 아이템입니다!");
            return false;
        }
    }

    // 현재 퀵 슬롯에 해당 아이템이 존재하면 사용
    public bool TryUseItemInQuickSlot(string itemID)
    {
        if (slots == null) return false;
        foreach (var slot in slots)
        {
            if (slot.IsEmptySlot()) continue;
            
            if (slot.current.itemId.Equals(itemID))
            {
                slot.Clear();
                return true;
            }
        }

        return false;
    }

    // 포커싱된 슬롯 색 변경
    public void UpdateSlotFocused(int index)
    {
        focusedIndex = index;

        for (int i = 0; i < MAX_SLOT_COUNT; i++)
        {
            if (i == index)
                slots[i].backgroundImage.color = Color.green;
            else
                slots[i].backgroundImage.color = Color.black;
        }

        if (slots[focusedIndex].current != null)
        {
            Item focusedItem = ItemManager.Instance.GetItemById(slots[focusedIndex].current.itemId);
            bool lampOn = focusedItem != null && focusedItem.IsLamp;
            Debug.Log("램프 온: " + lampOn);


            LampNet.SetLampOn(lampOn);
        }
        else
        {
            LampNet.SetLampOn(false);
        }
    }

    public void SaveSnapshotToProps()
    {
        var flat = PackSnapshotFlat();
        string joined = string.Join("|", flat);

        var ht = new PhotonHashtable
    {
        { "QS", joined }
    };

        PhotonNetwork.LocalPlayer.SetCustomProperties(ht);
    }



    public void SetActiveSlotParent(bool active) => quickSlotParent.SetActive(active);

    public void UpdateSlotData(int index, ItemInstance inst)
    {
        if (index < 0 || index >= MAX_SLOT_COUNT) return;

        slots[index].Clear();
        if (inst != null)
            slots[index].Set(inst);

        NotifySnapshotToMaster();
        SaveSnapshotToProps();
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
        if (focusedIndex < 0 || focusedIndex >= slots.Length) return null;
        return slots[focusedIndex];
    }
    public int GetMaxSlotCount() => MAX_SLOT_COUNT;

    public bool IsEmpty() => slots[focusedIndex].IsEmptySlot();

    public void AllClear()
    {
        foreach (var slot in slots)
            slot.Clear();

        NotifySnapshotToMaster();
        SaveSnapshotToProps();
    }
}

public static class QuickSlotNet
{
    public const byte EVT_QUICKSLOT_SNAPSHOT = 10;
}