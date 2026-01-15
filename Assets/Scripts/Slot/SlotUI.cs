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
        canvasGroup = FindAnyObjectByType<UIManager>().GetComponent<CanvasGroup>();
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
        
        TrySwap(from, this);
    }

    private void TrySwap(SlotUI from, SlotUI to)
    {
        var temp = to.CurrnetSlot;
        to.CurrnetSlot = from.CurrnetSlot;
        from.CurrnetSlot = temp;
    }
}
