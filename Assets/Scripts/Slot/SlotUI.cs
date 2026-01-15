using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Slot))]
public class SlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private CanvasGroup canvasGroup;
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
    }

    public void OnDrop(PointerEventData eventData)
    {
        var from = eventData.pointerDrag?.GetComponent<SlotUI>();

        if (from == null || from == this) return;
        if (from.CurrnetSlot.IsEmptySlot()) return;

        int fromIndex = from.CurrnetSlot.slotIndex;
        int toIndex = this.CurrnetSlot.slotIndex;

        InventoryManager.Instance.RequestMoveItem
        (
            from.CurrnetSlot.slotType, fromIndex,
            this.CurrnetSlot.slotType, toIndex
        );

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
}
