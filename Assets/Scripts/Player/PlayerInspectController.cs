using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerCameraController))]
public class PlayerInspectController : MonoBehaviourPun
{
    [SerializeField] private CinemachineBrain brain;
    private PlayerInput playerInput;
    private InputAction inspectAction;
    private PlayerCameraController camController;

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        camController = this.GetComponent<PlayerCameraController>();

        inspectAction = playerInput.actions["Inspect"];
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        inspectAction.performed += OnInspect;
    }

    private void OnDisable()
    {
        if (inspectAction != null)
            inspectAction.performed -= OnInspect;
    }

    public void OnInspect(InputAction.CallbackContext ctx)
    {
        if (camController != null)
            camController.SetBlendCut();

        var inspectMgr = InspectManager.Instance;
        if (inspectMgr.IsInspecting) inspectMgr.Exit();
        else inspectMgr.TryEnterFromFocusedSlot();
    }
}
