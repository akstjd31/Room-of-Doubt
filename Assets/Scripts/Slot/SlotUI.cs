using Microsoft.Unity.VisualStudio.Editor;
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
        Debug.Log("fdsfds");
        var from = eventData.pointerDrag?.GetComponent<SlotUI>();
        if (from == null || from == this) return;
        if (from.CurrnetSlot == null || from.CurrnetSlot.IsEmptySlot()) return;

        // this(드롭 받은 슬롯)가 비어있을 때만 이동
        if (CurrnetSlot.IsEmptySlot())
        {
            CurrnetSlot.AddItem(from.CurrnetSlot.currentItem);
            from.CurrnetSlot.ClearSlot();
        }
        else
        {
            // 비어있지 않으면 스왑(원하면)
            var temp = CurrnetSlot.currentItem;
            CurrnetSlot.ClearSlot();
            CurrnetSlot.AddItem(from.CurrnetSlot.currentItem);

            from.CurrnetSlot.ClearSlot();
            from.CurrnetSlot.AddItem(temp);
        }
    }

}
