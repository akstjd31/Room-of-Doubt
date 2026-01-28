using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
public class PlayerQuickSlotController : MonoBehaviourPunCallbacks
{
    private int selectedSlotIndex;
    private int maxSlotCount;
    private PlayerController playerController;
    private PlayerInput playerInput;
    private InputAction selectAction, scrollAction;

    public override void OnEnable()
    {
        base.OnEnable();
        
        if (!photonView.IsMine) return;

        selectAction.performed += OnSelectSlotPerformed;
        scrollAction.performed += OnScrollSlotPerformed;
    }

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        playerController = this.GetComponent<PlayerController>();

        selectAction = playerInput.actions["Select"];
        scrollAction = playerInput.actions["Scroll"];

        selectedSlotIndex = -1;
    }

    private void Start()
    {
        if (!photonView.IsMine) return;

        if (QuickSlotManager.Local != null)
            maxSlotCount = QuickSlotManager.Local.GetMaxSlotCount();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += DisableActions;
            GameManager.Instance.OnGameResumed += EnableActions;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        
        if (selectAction != null)
            selectAction.performed -= OnSelectSlotPerformed;
        
        if (scrollAction != null)
            scrollAction.performed -= OnScrollSlotPerformed;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGamePaused -= DisableActions;
        GameManager.Instance.OnGameResumed -= EnableActions;
    }

    private void DisableActions()
    {
        selectAction.Disable();
        scrollAction.Disable();
    }

    private void EnableActions()
    {
        selectAction.Enable();
        scrollAction.Enable();
    }

        // 숫자 버튼으로 슬롯 변경 방식
    private void OnSelectSlotPerformed(InputAction.CallbackContext ctx)
    {
        if (playerController.IsEscaped) return;
        if (UIManager.Instance.IsOpen) return;
        if (InspectManager.Instance.IsInspecting) return;
        if (GameManager.Instance.IsInteractingFocused) return;

        int nextIndex = int.Parse(ctx.control.name) - 1;
        
        if (nextIndex < 0 || nextIndex >= maxSlotCount) return;
        selectedSlotIndex = nextIndex;

        QuickSlotManager.Local.UpdateSlotFocused(selectedSlotIndex);
    }

    // 스크롤로 슬롯 변경 방식
    private void OnScrollSlotPerformed(InputAction.CallbackContext ctx)
    {
        if (playerController.IsEscaped) return;
        if (UIManager.Instance.IsOpen) return;
        if (InspectManager.Instance.IsInspecting) return;
        if (GameManager.Instance.IsInteractingFocused) return;
        
        float value = ctx.ReadValue<Vector2>().y;
        
        int delta = value > 0 ? 1 : -1;
        int nextIndex = selectedSlotIndex + delta;

        if (nextIndex < 0) nextIndex = maxSlotCount - 1;
        else if (nextIndex >= maxSlotCount) nextIndex = 0;
        
        selectedSlotIndex = nextIndex;
        QuickSlotManager.Local.UpdateSlotFocused(selectedSlotIndex);
    }
}
