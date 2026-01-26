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

    // =========================
    // Move 요청 (로컬에서 호출)
    // =========================
    public void RequestMoveItem(SlotType fromType, int fromIdx, SlotType toType, int toIdx)
    {
        // (A) from 쪽 아이템 스냅샷
        ItemInstance fromInst = null;
        if (fromType == SlotType.Quick)
            fromInst = QuickSlotManager.Local.GetItemInstanceByIndex(fromIdx);
        else
            fromInst = sharedItems[fromIdx];

        SplitInstance(fromInst, out string fromItemId, out string fromHintKey, out string fromPayload);

        // (B) Inventory -> Quick 인 경우, "퀵슬롯 목적지(toIdx)에 현재 들어있는 아이템"도 같이 보낸다 (스왑용)
        string quickOldItemId = "";
        string quickOldHintKey = "";
        string quickOldPayload = "";

        if (fromType == SlotType.Inventory && toType == SlotType.Quick)
        {
            var quickOld = QuickSlotManager.Local.GetItemInstanceByIndex(toIdx);
            SplitInstance(quickOld, out quickOldItemId, out quickOldHintKey, out quickOldPayload);
        }

        photonView.RPC(nameof(RequestMoveRPC), RpcTarget.MasterClient,
            fromType, fromIdx, toType, toIdx,
            fromItemId, fromHintKey, fromPayload,
            quickOldItemId, quickOldHintKey, quickOldPayload);
    }

    // =========================
    // Move 처리 (마스터에서)
    // =========================
    [PunRPC]
    private void RequestMoveRPC(
        SlotType fromType, int fromIdx,
        SlotType toType, int toIdx,
        string fromItemId, string fromHintKey, string fromPayload,           // from 슬롯에 있던 아이템(요청자가 보냄)
        string quickOldItemId, string quickOldHintKey, string quickOldPayload, // Inventory->Quick 스왑에 필요한 quick 목적지 기존 아이템(요청자가 보냄)
        PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // from 인스턴스 복원(Quick->Inventory에서는 이게 movingInst 역할)
        ItemInstance movingInst = MakeInstance(fromItemId, fromHintKey, fromPayload);

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

            // 마스터 기준 인벤에서 꺼낼 아이템
            ItemInstance sendingInst = sharedItems[fromIdx];

            // 요청자가 보내준 "퀵슬롯 목적지 기존 아이템"을 인벤으로 되돌려 넣음(스왑)
            ItemInstance quickOldInst = MakeInstance(quickOldItemId, quickOldHintKey, quickOldPayload);
            sharedItems[fromIdx] = quickOldInst;

            // 인벤 동기화
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, Flatten(sharedItems));

            // 요청자 퀵슬롯에는 인벤에서 꺼낸 아이템을 넣어줌
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
    public void ResponseQuickSlotUpdate(int slotIndex, string itemId, string hintKey, string payload)
    {
        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        var inst = string.IsNullOrEmpty(itemId)
            ? null
            : new ItemInstance(itemId, hint);

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
        Debug.Log($"[Absorb CALL] isMaster={PhotonNetwork.IsMasterClient}, len={(snapshotFlat==null ? -1 : snapshotFlat.Length)}");

        if (!PhotonNetwork.IsMasterClient) return;
        if (snapshotFlat == null || snapshotFlat.Length == 0) return;

        // 3개 단위( itemId, hintKey, payload )
        int slotCount = snapshotFlat.Length / 3;

        for (int i = 0; i < slotCount; i++)
        {
            int baseIdx = i * 3;

            string itemId = snapshotFlat[baseIdx + 0];
            string hintKey = snapshotFlat[baseIdx + 1];
            string payload = snapshotFlat[baseIdx + 2];

            if (string.IsNullOrEmpty(itemId))
                continue;

            ItemInstance inst = MakeInstance(itemId, hintKey, payload);

            if (!InsertFirstEmpty(inst))
            {
                Debug.LogWarning("[SharedInventory] Inventory full. Some items could not be absorbed.");
                break;
            }
        }
        
        // 공유 인벤토리니 RPC 수행
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
    private ItemInstance MakeInstance(string itemId, string hintKey, string payload)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        var hint = string.IsNullOrEmpty(hintKey)
            ? HintData.Empty
            : new HintData { hintKey = hintKey, payload = payload };

        return new ItemInstance(itemId, hint);
    }

    // 평면 형태의 stirng 배열로 바꾸기 (아이템 인스턴스에서)
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

    // Flatten으로 바뀐 string 배열을 슬롯 배열(ItemInstance)로 반환
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

    // 전송이 가능한 문자열 데이터로 나누기
    private void SplitInstance(ItemInstance inst, out string itemId, out string hintKey, out string payload)
    {
        if (inst == null)
        {
            itemId = "";
            hintKey = "";
            payload = "";
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
    }
}
