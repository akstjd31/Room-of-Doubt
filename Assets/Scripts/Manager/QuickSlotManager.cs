using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(PlayerInput))]
public class QuickSlotManager : MonoBehaviourPun
{
    const int MAX_SLOT_COUNT = 4;

    [Header("Slot")]
    [SerializeField] private GameObject slot;
    public Slot[] slots;
    public int selectedSlotIndex;

    private PlayerInput playerInput;
    private InputAction selectAction, scrollAction;

    private void Awake()
    {
        if (!photonView.IsMine) return;

        playerInput = this.GetComponent<PlayerInput>();
        selectAction = playerInput.actions["Select"];
        scrollAction = playerInput.actions["Scroll"];

        slots = new Slot[MAX_SLOT_COUNT];

        if (slot != null)
        {
            GameObject quickSlotObj = GameObject.FindGameObjectWithTag("QuickSlot");

            for (int i = 0; i < MAX_SLOT_COUNT; i++)
            {
                GameObject newSlotObj = Instantiate(slot, quickSlotObj.transform.GetChild(0));
                slots[i] = newSlotObj.GetComponent<Slot>();
            }
        }

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

    private void UpdateSlotFocusing(int index)
    {
        for (int i = 0; i < MAX_SLOT_COUNT; i++)
        {
            if (i == index)
                slots[i].backgroundImage.color = Color.green;
            else
                slots[i].backgroundImage.color = Color.black;
        }
    }

    private void OnSelectSlotPerformed(InputAction.CallbackContext ctx)
    {
        int nextIndex = int.Parse(ctx.control.name) - 1;
        
        if (nextIndex < 0 || nextIndex >= MAX_SLOT_COUNT) return;
        selectedSlotIndex = nextIndex;

        UpdateSlotFocusing(selectedSlotIndex);
    }

    private void OnScrollSlotPerformed(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<Vector2>().y;
        
        int delta = value > 0 ? 1 : -1;
        int nextIndex = selectedSlotIndex + delta;

        if (nextIndex < 0) nextIndex = MAX_SLOT_COUNT - 1;
        else if (nextIndex >= MAX_SLOT_COUNT) nextIndex = 0;
        
        selectedSlotIndex = nextIndex;
        UpdateSlotFocusing(selectedSlotIndex);
    }
}
