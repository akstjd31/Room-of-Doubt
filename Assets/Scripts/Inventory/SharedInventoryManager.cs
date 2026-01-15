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
    public string[] sharedItems;
    [SerializeField] private int inventorySize = 20;
    private List<Slot> slots;
    private void Awake()
    {
        Instance = this;
        sharedItems = new string[inventorySize];
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
                sharedItems[i] = "";
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
        string fromItemId = "";

        // 퀵 슬롯에서부터 시작된다면 아이템 ID를 가져옴
        if (fromType.Equals(SlotType.Quick))
            fromItemId = QuickSlotManager.Instance.GetItemIdByIndex(fromIdx);
        else
            fromItemId = sharedItems[fromIdx];

        // 마스터 클라이언트에서 해당 데이터 처리
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
            if (toIdx < 0 || toIdx >= sharedItems.Length) return;

            string targetOldItem = sharedItems[toIdx];
            sharedItems[toIdx] = itemID;

            // 공용 인벤토리 동기화 (원격)
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems);

            // 퀵 슬롯 결과 전송 (로컬)
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, fromIdx, targetOldItem);
            Debug.Log($"아이템 이동: 퀵 슬롯 -> 인벤토리");
        }

        // 2. 인벤토리 -> 퀵 슬롯인 경우
        else if (fromType.Equals(SlotType.Inventory) && toType.Equals(SlotType.Quick))
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length) return;
            
            // 기존 데이터 제거 및 결과를 전송할 데이터(sendingItem) 임시 저장
            string sendingItem = sharedItems[fromIdx];
            sharedItems[fromIdx] = QuickSlotManager.Instance.GetItemIdByIndex(toIdx);

            // 인벤토리의 변화가 생길때마다 노티해줌 (원격)
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems);

            // 개인 퀵 슬롯으로 가져오는 작업 (로컬)
            photonView.RPC(nameof(ResponseQuickSlotUpdate), info.Sender, toIdx, sendingItem);
            Debug.Log($"아이템 이동: 인벤토리 -> 퀵 슬롯");
        }

        // 3. 인벤토리 -> 인벤토리인 경우
        else
        {
            if (fromIdx < 0 || fromIdx >= sharedItems.Length || toIdx < 0 || toIdx >= sharedItems.Length) return;

            // 스왑
            (sharedItems[toIdx], sharedItems[fromIdx]) = (sharedItems[fromIdx], sharedItems[toIdx]);
            
            // 변경 결과 전송 (원격)
            photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, sharedItems);

            if (fromType.Equals(SlotType.Inventory) && toType.Equals(SlotType.Inventory))
                Debug.Log($"아이템 이동: 인벤토리 -> 인벤토리");
            else
                Debug.Log($"아이템 이동: 퀵 슬롯 -> 퀵 슬롯");
        }
    }

    // 결과를 매개변수로 받아 저장 및 갱신
    [PunRPC]
    private void SyncInventoryRPC(string[] updatedItems)
    {
        for (int i = 0; i < updatedItems.Length; i++)
        {
            sharedItems[i] = updatedItems[i];
        }

        UpdateInventoryUI();
    }

    // 퀵 슬롯 갱신 작업 (여기서 PRC를 호출한 이유는 퀵 슬롯에서 하면 퀵 슬롯 포톤 뷰 RPC로 동작해야한다는 점 떄문에)
    [PunRPC]
    public void ResponseQuickSlotUpdate(int slotIndex, string newItemID)
    {
        QuickSlotManager.Instance.UpdateSlotData(slotIndex, newItemID);
    }

    // 인벤토리 갱신
    private void UpdateInventoryUI()
    {
        for (int i = 0; i < sharedItems.Length; i++)
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
