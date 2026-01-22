using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using WebSocketSharp;

// 현재 보관하고 있는 아이템 관리
// 아이템이 추가, 제거, 탐색 가능한 기능
public class SharedInventoryManager : MonoBehaviourPunCallbacks
{
    public static SharedInventoryManager Instance;
    [Header("References")]
    [SerializeField] private GameObject panelObj;
    [SerializeField] private GameObject bagObj;
    [SerializeField] private Transform slotPrefab;

    [Header("Slot")]
    public ItemInstance[] sharedItems;
    [SerializeField] private int inventorySize = 20;
    private List<Slot> slots;
    private void Awake()
    {
        Instance = this;
        sharedItems = new ItemInstance[inventorySize];
        slots = new List<Slot>();

        if (bagObj != null)
        {
            for (int i = 0; i < inventorySize; i++)
            {
                GameObject newSlot = Instantiate(slotPrefab.gameObject, bagObj.transform);
                Slot slot = newSlot.GetComponent<Slot>();
                slot.slotType = SlotType.Inventory;
                slot.slotIndex = i;

                slots.Add(slot);
                sharedItems[i] = null;
            }
        }
    }

    private void Start()
    {
        // UIManager.Instance.OnInvenOpened += UpdateInventory;
    }

    private void OnDestroy()
    {
        // UIManager.Instance.OnInvenOpened -= UpdateInventory;
    }

    public void SetPanelActive(bool active) => panelObj.SetActive(active);

    // 아이템 이동 요청 (로컬에서 호출)
    public void RequestMoveItem(SlotType fromType, int fromIdx, SlotType toType, int toIdx)
    {
        // 이동하는 "인스턴스" 꺼내기
        ItemInstance inst = null;

        if (fromType == SlotType.Quick)
            inst = QuickSlotManager.Instance.GetItemInstanceByIndex(fromIdx);
        else
            inst = sharedItems[fromIdx];

        SplitInstance(inst, out string itemId, out string hintKey, out string payload);

        photonView.RPC(nameof(RequestMoveRPC), RpcTarget.MasterClient,
            fromType, fromIdx, toType, toIdx,
            itemId, hintKey, payload);
    }


    [PunRPC]
    private void RequestMoveRPC(
    SlotType fromType, int fromIdx,
    SlotType toType, int toIdx,
    string itemId, string hintKey, string payload,
    PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        ItemInstance movingInst = MakeInstance(itemId, hintKey, payload);

        // 1) Quick -> Inventory
        if (fromType == SlotType.Quick && toType == SlotType.Inventory)
        {
            if (toIdx < 0 || toIdx >= sharedItems.Length) return;

            ItemInstance oldInvInst = sharedItems[toIdx];
            sharedItems[toIdx] = movingInst;

            // 인벤 전체 동기화
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));

            // 퀵슬롯에는 인벤에 있던 것(스왑 결과)을 돌려줌
            SplitInstance(oldInvInst, out var retItemId, out var retHintKey, out var retPayload);
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, fromIdx, retItemId, retHintKey, retPayload);

            return;
        }

        // 2) Inventory -> Quick
        if (fromType == SlotType.Inventory && toType == SlotType.Quick)
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length) return;

            ItemInstance sendingInst = sharedItems[fromIdx];

            // 퀵슬롯 기존 아이템을 인벤으로 되돌려 넣고
            ItemInstance quickOldInst = QuickSlotManager.Instance.GetItemInstanceByIndex(toIdx);
            sharedItems[fromIdx] = quickOldInst;

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));

            // 퀵슬롯에는 인벤에서 꺼낸 것을 넣어줌
            SplitInstance(sendingInst, out var sendItemId, out var sendHintKey, out var sendPayload);
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, toIdx, sendItemId, sendHintKey, sendPayload);

            return;
        }

        // 3) Inventory <-> Inventory
        if (fromType == SlotType.Inventory && toType == SlotType.Inventory)
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length) return;
            if (toIdx < 0 || toIdx >= sharedItems.Length) return;

            (sharedItems[toIdx], sharedItems[fromIdx]) = (sharedItems[fromIdx], sharedItems[toIdx]);

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));
            return;
        }

        // (Quick <-> Quick 은 사실 여기서 처리할 필요 없음. 로컬 퀵슬롯 UI 이동이면 네트워크 불필요)
    }



    // 결과를 매개변수로 받아 저장 및 갱신
    [PunRPC]
    private void SyncInventoryRPC(string[] flat)
    {
        sharedItems = Unflatten(flat);
        UpdateInventoryUI();
    }

    // 퀵 슬롯 갱신 작업 (여기서 PRC를 호출한 이유는 퀵 슬롯에서 하면 퀵 슬롯 포톤 뷰 RPC로 동작해야한다는 점 떄문에)
    [PunRPC]
    public void ResponseQuickSlotUpdate(int slotIndex, string itemId, string hintKey, string payload)
    {
        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        var inst = string.IsNullOrEmpty(itemId)
            ? null
            : new ItemInstance(itemId, hint);
            
        QuickSlotManager.Instance.UpdateSlotData(slotIndex, inst);
    }

    // 인벤토리 갱신
    private void UpdateInventoryUI()
    {
        for (int i = 0; i < sharedItems.Length; i++)
        {
            var inst = sharedItems[i];

            if (inst == null)
            {
                if (!slots[i].IsEmptySlot())
                    slots[i].Clear();
                continue;
            }

            slots[i].Set(inst);

            // 만약 인벤 슬롯 UI에서도 "힌트 존재" 표시가 필요하면
            // inst.hint.HasValue 로 뱃지/아이콘 표시 가능
        }
    }


    private ItemInstance MakeInstance(string itemId, string hintKey, string payload)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        return new ItemInstance(itemId, hint);
    }

    private string[] Flatten(ItemInstance[] arr)
    {
        var flat = new string[arr.Length * 3];
        for (int i = 0; i < arr.Length; i++)
        {
            SplitInstance(arr[i], out string itemId, out string hintKey, out string payload);
            int baseIdx = i * 3;
            flat[baseIdx + 0] = itemId ?? "";
            flat[baseIdx + 1] = hintKey ?? "";
            flat[baseIdx + 2] = payload ?? "";
        }
        return flat;
    }

    private ItemInstance[] Unflatten(string[] flat)
    {
        int len = flat.Length / 3;
        var arr = new ItemInstance[len];
        for (int i = 0; i < len; i++)
        {
            int baseIdx = i * 3;
            string itemId = flat[baseIdx + 0];
            string hintKey = flat[baseIdx + 1];
            string payload = flat[baseIdx + 2];
            arr[i] = MakeInstance(itemId, hintKey, payload);
        }
        return arr;
    }


    private void SplitInstance(ItemInstance inst, out string itemId, out string hintKey, out string payload)
    {
        if (inst == null)
        {
            itemId = "";
            hintKey = "";
            payload = "";
            return;
        }

        itemId = inst.itemId;
        if (inst.hint.HasValue)
        {
            hintKey = inst.hint.hintKey;
            payload = inst.hint.payload;
        }
        else
        {
            hintKey = "";
            payload = "";
        }
    }

}
