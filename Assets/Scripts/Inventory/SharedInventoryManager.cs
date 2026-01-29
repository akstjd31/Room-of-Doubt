using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

// 현재 보관하고 있는 아이템 관리
// 아이템이 추가, 제거, 탐색 가능한 기능
public class SharedInventoryManager : MonoBehaviourPunCallbacks
{
    public static SharedInventoryManager Instance;

    private const int FIELDS_PER_SLOT = 4; // itemId, hintKey, payload, installedPartId


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

    public void SetPanelActive(bool active) => panelObj.SetActive(active);

    // 아이템 이동 시 마스터한테 요청
    public void RequestMoveItem(SlotType fromType, int fromIdx, SlotType toType, int toIdx)
    {
        ItemInstance fromInst = null;
        if (fromType == SlotType.Quick)
            fromInst = QuickSlotManager.Local.GetItemInstanceByIndex(fromIdx);
        else
            fromInst = sharedItems[fromIdx];

        SplitInstance(fromInst, out string fromItemId, out string fromHintKey, out string fromPayload, out string fromInstalledPartId);

        // Inventory -> Quick 스왑용: 목적지 퀵슬롯 기존 아이템도 같이
        string quickOldItemId = "";
        string quickOldHintKey = "";
        string quickOldPayload = "";
        string quickOldInstalledPartId = "";

        // 근데 만약 인벤토리에서 퀵 슬롯으로 이동할 땐?
        if (fromType == SlotType.Inventory && toType == SlotType.Quick)
        {
            var quickOld = QuickSlotManager.Local.GetItemInstanceByIndex(toIdx);
            SplitInstance(quickOld, out quickOldItemId, out quickOldHintKey, out quickOldPayload, out quickOldInstalledPartId);
        }

        photonView.RPC(nameof(RequestMoveRPC), RpcTarget.MasterClient,
            fromType, fromIdx, toType, toIdx,
            fromItemId, fromHintKey, fromPayload, fromInstalledPartId,
            quickOldItemId, quickOldHintKey, quickOldPayload, quickOldInstalledPartId);
    }


    [PunRPC]
    private void RequestMoveRPC(
    SlotType fromType, int fromIdx,
    SlotType toType, int toIdx,
    string fromItemId, string fromHintKey, string fromPayload, string fromInstalledPartId,
    string quickOldItemId, string quickOldHintKey, string quickOldPayload, string quickOldInstalledPartId,
    PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // from 인스턴스 복원(Quick->Inventory에서는 movingInst 역할)
        ItemInstance movingInst = MakeInstance(fromItemId, fromHintKey, fromPayload, fromInstalledPartId);

        // 1) 퀵 -> 인벤
        if (fromType == SlotType.Quick && toType == SlotType.Inventory)
        {
            if (toIdx < 0 || toIdx >= sharedItems.Length) return;

            ItemInstance oldInvInst = sharedItems[toIdx];
            sharedItems[toIdx] = movingInst;

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));

            // 퀵슬롯에는 인벤에 있던 것(스왑 결과)을 돌려줌
            SplitInstance(oldInvInst, out var retItemId, out var retHintKey, out var retPayload, out var retInstalledPartId);
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, fromIdx, retItemId, retHintKey, retPayload, retInstalledPartId);
            return;
        }

        // 2) 인벤 -> 퀵
        if (fromType == SlotType.Inventory && toType == SlotType.Quick)
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length) return;

            ItemInstance sendingInst = sharedItems[fromIdx];

            // 요청자가 보내준 "퀵슬롯 목적지 기존 아이템"을 인벤으로 되돌려 넣음(스왑)
            ItemInstance quickOldInst = MakeInstance(quickOldItemId, quickOldHintKey, quickOldPayload, quickOldInstalledPartId);
            sharedItems[fromIdx] = quickOldInst;

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));

            // 요청자 퀵슬롯에는 인벤에서 꺼낸 아이템을 넣어줌
            SplitInstance(sendingInst, out var sendItemId, out var sendHintKey, out var sendPayload, out var sendInstalledPartId);
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, toIdx, sendItemId, sendHintKey, sendPayload, sendInstalledPartId);
            return;
        }

        // 3) 인벤 <-> 인벤 왔다갔다
        if (fromType == SlotType.Inventory && toType == SlotType.Inventory)
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length) return;
            if (toIdx < 0 || toIdx >= sharedItems.Length) return;

            (sharedItems[toIdx], sharedItems[fromIdx]) = (sharedItems[fromIdx], sharedItems[toIdx]);
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));
            return;
        }
    }


    // 인벤토리 동기화
    [PunRPC]
    private void SyncInventoryRPC(string[] flat)
    {
        sharedItems = Unflatten(flat);
        UpdateInventoryUI();
    }

    // 내 퀵슬롯 갱신
    [PunRPC]
    public void ResponseQuickSlotUpdate(int slotIndex, string itemId, string hintKey, string payload, string installedPartId)
    {
        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        var inst = string.IsNullOrEmpty(itemId)
            ? null
            : new ItemInstance(itemId, hint);

        if (inst != null)
            inst.installedPartId = string.IsNullOrEmpty(installedPartId) ? null : installedPartId;

        QuickSlotManager.Local.UpdateSlotData(slotIndex, inst);
    }


    // 인벤 갱신
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
        }
    }

    // 나간 플레이어 전용 퀵 슬롯을 인벤에 옮기기
    public void AbsorbPackedQuickSlots(string[] snapshotFlat)
    {
        Debug.Log("플레이어에 있던 퀵 슬롯 인벤에 옮겨야 됨!");

        if (!PhotonNetwork.IsMasterClient) return;
        if (snapshotFlat == null || snapshotFlat.Length == 0) return;

        int slotCount = snapshotFlat.Length / FIELDS_PER_SLOT;

        for (int i = 0; i < slotCount; i++)
        {
            int baseIdx = i * FIELDS_PER_SLOT;

            string itemId = snapshotFlat[baseIdx + 0];
            string hintKey = snapshotFlat[baseIdx + 1];
            string payload = snapshotFlat[baseIdx + 2];
            string installedPartId = snapshotFlat[baseIdx + 3];

            if (string.IsNullOrEmpty(itemId))
                continue;

            ItemInstance inst = MakeInstance(itemId, hintKey, payload, installedPartId);

            if (!InsertFirstEmpty(inst))
            {
                Debug.LogWarning("[SharedInventory] Inventory full. Some items could not be absorbed.");
                break;
            }
        }

        photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));
    }


    private bool InsertFirstEmpty(ItemInstance inst)
    {
        for (int i = 0; i < sharedItems.Length; i++)
        {
            if (sharedItems[i] != null) continue;
            sharedItems[i] = inst;
            return true;
        }
        return false;
    }

    // 아이템 인스턴스 형태로 만들기 (슬롯 복원)
    private ItemInstance MakeInstance(string itemId, string hintKey, string payload, string installedPartId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        var inst = new ItemInstance(itemId, hint);
        inst.installedPartId = string.IsNullOrEmpty(installedPartId) ? null : installedPartId;
        return inst;
    }


    // 평면 형태의 stirng 배열로 바꾸기 (아이템 인스턴스에서)
    private string[] Flatten(ItemInstance[] arr)
    {
        var flat = new string[arr.Length * FIELDS_PER_SLOT];

        for (int i = 0; i < arr.Length; i++)
        {
            SplitInstance(arr[i],
                out string itemId, out string hintKey, out string payload, out string installedPartId);

            int baseIdx = i * FIELDS_PER_SLOT;
            flat[baseIdx + 0] = itemId ?? "";
            flat[baseIdx + 1] = hintKey ?? "";
            flat[baseIdx + 2] = payload ?? "";
            flat[baseIdx + 3] = installedPartId ?? "";
        }
        return flat;
    }


    // Flatten으로 바뀐 string 배열을 슬롯 배열(ItemInstance)로 반환
    private ItemInstance[] Unflatten(string[] flat)
    {
        int len = flat.Length / FIELDS_PER_SLOT;
        var arr = new ItemInstance[len];

        for (int i = 0; i < len; i++)
        {
            int baseIdx = i * FIELDS_PER_SLOT;

            string itemId = flat[baseIdx + 0];
            string hintKey = flat[baseIdx + 1];
            string payload = flat[baseIdx + 2];
            string installedPartId = flat[baseIdx + 3];

            arr[i] = MakeInstance(itemId, hintKey, payload, installedPartId);
        }
        return arr;
    }


    // 전송이 가능한 문자열 데이터로 나누기
    private void SplitInstance(ItemInstance inst,
        out string itemId, out string hintKey, out string payload, out string installedPartId)
    {
        if (inst == null)
        {
            itemId = "";
            hintKey = "";
            payload = "";
            installedPartId = "";
            return;
        }

        itemId = inst.itemId ?? "";

        if (inst.hint.HasValue)
        {
            hintKey = inst.hint.hintKey ?? "";
            payload = inst.hint.payload ?? "";
        }
        else
        {
            hintKey = "";
            payload = "";
        }

        installedPartId = inst.installedPartId ?? "";
    }
}
