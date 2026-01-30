using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Unity.Cinemachine;

[RequireComponent(typeof(Slot))]
public class SlotUI : MonoBehaviourPun,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private CanvasGroup canvasGroup;
    // [SerializeField] private Camera worldRayCamera;     
    [SerializeField] private LayerMask interactableMask;
    public Slot CurrnetSlot { get; private set; }
    private Transform originalParent;
    private DragIcon dragIcon;

    private void Awake()
    {
        CurrnetSlot = this.GetComponent<Slot>();
        canvasGroup = this.GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CurrnetSlot.IsEmptySlot()) return;

        UIDragState.Begin(this);

        dragIcon = DragIcon.Instance;
        dragIcon.Show(CurrnetSlot.iconImage.sprite);

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        dragIcon.Follow(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) dragIcon.Hide();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        bool droppedOnSlotUI = eventData.pointerEnter != null &&
                                eventData.pointerEnter.GetComponentInParent<SlotUI>() != null;

        if (!droppedOnSlotUI)
        {
            TryUseOnWorld(eventData);
        }

        UIDragState.End();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var from = eventData.pointerDrag?.GetComponent<SlotUI>();

        if (from == null || from == this) return;
        if (from.CurrnetSlot.IsEmptySlot()) return;

        int fromIndex = from.CurrnetSlot.slotIndex;
        int toIndex = this.CurrnetSlot.slotIndex;

        SharedInventoryManager.Instance.RequestMoveItem
        (
            from.CurrnetSlot.slotType, fromIndex,
            this.CurrnetSlot.slotType, toIndex
        );

        // 만약 인벤토리 자체를 로컬에서 관리하고 싶다면 아래 코드를 사용 ▼
        // this(드롭 받은 슬롯)가 비어있을 때만 이동
        // if (CurrnetSlot.IsEmptySlot())
        // {
        //     CurrnetSlot.AddItem(from.CurrnetSlot.currentItem);
        //     from.CurrnetSlot.ClearSlot();

        //     // 현재 슬롯이 인벤토리쪽 슬롯이라면 인벤토리 리스트 추가
        //     // if (CurrnetSlot.slotType.Equals(SlotType.Inventory))
        //     //     InventoryManager.Instance.AddItem(CurrnetSlot.currentItem);
        //     // else
        //     //     InventoryManager.Instance.RemoveItem(CurrnetSlot.currentItem);
        // }
        // else
        // {
        //     // 비어있지 않으면 스왑(원하면)
        //     var temp = CurrnetSlot.currentItem;
        //     CurrnetSlot.ClearSlot();
        //     CurrnetSlot.AddItem(from.CurrnetSlot.currentItem);

        //     from.CurrnetSlot.ClearSlot();
        //     from.CurrnetSlot.AddItem(temp);
        // }
    }

    private void TryUseOnWorld(PointerEventData eventData)
    {
        var brain = Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain == null) return;

        if (CurrnetSlot == null || CurrnetSlot.IsEmptySlot()) return;

        var inst = CurrnetSlot.current;
        if (inst == null || string.IsNullOrEmpty(inst.itemId)) return;

        // 현재 아이템을 사용할 수 있는지? (부품이 필요한지? 필요하다면 해당 부품이 끼워져 있는지?)
        if (!QuickSlotManager.Local.CanUseItem(inst))
        {
            UIManager.Instance.ShowMessage("부품이 필요합니다!");
            return;
        }

        Camera cam = brain.OutputCamera != null ? brain.OutputCamera : Camera.main;
        Ray ray = cam.ScreenPointToRay(eventData.position);

        if (!Physics.Raycast(ray, out var hit, 100f, interactableMask))
            return;

        var target = hit.collider.GetComponent<InteractableBase>();
        if (target == null) return;

        if (target.TryInstallToHost(inst, out var reason))
        {
            QuickSlotManager.Local.UpdateSlotData(CurrnetSlot.slotIndex, null);

            if (target is Remote remote)
                remote.RefreshTapeState();
        }
        else
        {
            UIManager.Instance.ShowMessage(reason);
        }
    }
}
