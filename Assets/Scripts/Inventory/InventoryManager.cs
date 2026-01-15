using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

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
    public List<int> itemDatabase;
    [SerializeField] private int slotCount = 20;
    private List<Slot> slots;
    private void Awake()
    {
        Instance = this;
        itemDatabase = new List<int>();
        slots = new List<Slot>();

        if (bagObj != null)
        {
            for (int i = 0; i < slotCount; i++)
            {
                GameObject newSlot = Instantiate(slotPrefab.gameObject, bagObj.transform);
                Slot slot = newSlot.GetComponent<Slot>();
                slot.slotType = SlotType.Inventory;
                slot.slotIndex = i;

                slots.Add(slot);
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
        photonView.RPC(nameof(RequestMoveRPC), RpcTarget.MasterClient,
                        fromType, fromIdx,
                        toType, toIdx);
    }

    [PunRPC]
    private void RequestMoveRPC(SlotType fromType, int fromIdx, SlotType toType, int toIdx)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 스왑
        int temp = itemDatabase[toIdx];
        itemDatabase[toIdx] = itemDatabase[fromIdx];
        itemDatabase[fromIdx] = temp;

        photonView.RPC(nameof(SyncInventoryRPC), RpcTarget.All, itemDatabase.ToArray());
    }

    [PunRPC]
    private void SyncInventoryRPC(int[] updatedItems)
    {
        for (int i = 0; i < updatedItems.Length; i++)
        {
            itemDatabase[i] = updatedItems[i];
            UpdateSlotUI(i, updatedItems[i]);
        }
    }

    private void UpdateSlotUI(int index, int itemId)
    {
        if (itemId == 0) slots[index].ClearSlot();
        else
        {
            
        }
    }
}
