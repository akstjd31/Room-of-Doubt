using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using WebSocketSharp;

// 현재 보관하고 있는 아이템 관리
// 아이템이 추가, 제거, 탐색 가능한 기능
public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager Instance;
    [Header("References")]
    [SerializeField] private GameObject panelObj;
    [SerializeField] private GameObject bagObj;
    [SerializeField] private Transform slotPrefab;

    [Header("Slot")]
    public List<string> sharedItems;
    [SerializeField] private int inventorySize = 20;
    private List<Slot> slots;
    private void Awake()
    {
        Instance = this;
        sharedItems = new List<string>();
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
                sharedItems.Add("");
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

    public void RequestMoveItem(SlotType fromType, int fromIdx, SlotType toType, int toIdx)
    {
        string fromItemId = "";

        // 퀵 슬롯에서부터 시작된다면 아이템 ID를 가져옴
        if (fromType.Equals(SlotType.Quick))
            fromItemId = QuickSlotManager.Instance.GetItemIdByIndex(fromIdx);
        else
            fromItemId = sharedItems[fromIdx];

        photonView.RPC(nameof(RequestMoveRPC), RpcTarget.MasterClient,
                        fromType, fromIdx,
                        toType, toIdx, fromItemId);
    }

    [PunRPC]
    private void RequestMoveRPC(SlotType fromType, int fromIdx,
        SlotType toType, int toIdx, string itemID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 1. 퀵 슬롯 -> 인벤토리인 경우
        if (fromType.Equals(SlotType.Quick) && toType.Equals(SlotType.Inventory))
        {
            if (toIdx < 0 || toIdx >= sharedItems.Count) return;

            string targetOldItem = sharedItems[toIdx];
            sharedItems[toIdx] = itemID;

            // 공용 인벤토리 동기화
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems.ToArray());

            // 퀵 슬롯 결과 전송 (로컬만)
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, fromIdx, targetOldItem);
            Debug.Log($"아이템 이동: 퀵 슬롯 -> 인벤토리");
        }

        // 2. 인벤토리 -> 퀵 슬롯인 경우
        else if (fromType.Equals(SlotType.Inventory) && toType.Equals(SlotType.Quick))
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Count) return;
            
            string sendingItem = sharedItems[fromIdx];
            sharedItems[fromIdx] = "";

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems.ToArray());
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, toIdx, sendingItem);
            Debug.Log($"아이템 이동: 인벤토리 -> 퀵 슬롯");
        }

        // 3. 인벤토리 -> 인벤토리인 경우
        else
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Count || toIdx < 0 || toIdx >= sharedItems.Count) return;


            (sharedItems[toIdx], sharedItems[fromIdx]) = (sharedItems[fromIdx], sharedItems[toIdx]);

            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems.ToArray());

            if (fromType.Equals(SlotType.Inventory) && toType.Equals(SlotType.Inventory))
                Debug.Log($"아이템 이동: 인벤토리 -> 인벤토리");
            else
                Debug.Log($"아이템 이동: 퀵 슬롯 -> 퀵 슬롯");
        }
    }

    [PunRPC]
    private void SyncInventoryRPC(string[] updatedItems)
    {
        for (int i = 0; i < updatedItems.Length; i++)
        {
            sharedItems[i] = updatedItems[i];
        }

        UpdateInventoryUI();
    }

    [PunRPC]
    public void ResponseQuickSlotUpdate(int slotIndex, string newItemID)
    {
        QuickSlotManager.Instance.UpdateSlotData(slotIndex, newItemID);
    }

    private void UpdateInventoryUI()
    {
        for (int i = 0; i < sharedItems.Count; i++)
        {
            if (sharedItems[i].IsNullOrEmpty())
            {
                if (!slots[i].IsEmptySlot())
                    slots[i].ClearSlot();

                continue;
            }

            Item item = ItemManager.Instance.GetItemById(sharedItems[i]);
            slots[i].AddItem(item);
        }
    }
}
