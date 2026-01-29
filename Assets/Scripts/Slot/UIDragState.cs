public static class UIDragState
{
    public static bool IsDragging { get; private set; }
    public static SlotUI DraggingSlotUI { get; private set; }

    public static void Begin(SlotUI slotUI)
    {
        IsDragging = true;
        DraggingSlotUI = slotUI;
    }

    public static void End()
    {
        IsDragging = false;
        DraggingSlotUI = null;
    }
}
