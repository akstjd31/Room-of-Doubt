using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(PlayerInput))]
public class QuickSlotManager : MonoBehaviourPun
{
    const int MAX_SLOT_COUNT = 4;

    [Header("Slot")]
    public Slot[] slots;
    public int selectedSlotIndex;

    private PlayerInput playerInput;
    private InputAction selectAction, scrollAction;

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        selectAction = playerInput.actions["Select"];
        scrollAction = playerInput.actions["Scroll"];

        slots = new Slot[MAX_SLOT_COUNT];
        selectedSlotIndex = -1;
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        selectAction.performed += OnSelectSlotPerformed;
        scrollAction.performed += OnScrollSlotPerformed;
    }

    private void OnDisable()
    {
        if (selectAction != null)
            selectAction.performed -= OnSelectSlotPerformed;
        
        if (scrollAction != null)
            scrollAction.performed -= OnScrollSlotPerformed;
    }

    private void OnSelectSlotPerformed(InputAction.CallbackContext ctx)
    {
        int nextIndex = int.Parse(ctx.control.name) - 1;
        
        if (nextIndex < 0 || nextIndex >= MAX_SLOT_COUNT) return;
        selectedSlotIndex = nextIndex;
    }

    private void OnScrollSlotPerformed(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<Vector2>().y;
        
        int delta = value > 0 ? 1 : -1;
        int nextIndex = selectedSlotIndex + delta < 0 ? MAX_SLOT_COUNT - 1 : 0;
        selectedSlotIndex = nextIndex;
    }
}
