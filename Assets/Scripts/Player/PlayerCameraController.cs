using System;
using System.Collections;
using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
public class PlayerCameraController : MonoBehaviourPun
{
    private PlayerController playerController;
    private PlayerInput playerInput;
    private InputAction lookAction;
    private Rigidbody rigid;

    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private GameObject playerMesh;

    [Header("Local Camera")]
    [SerializeField] private GameObject cameraRoot;
    private CinemachineBrain brain;
    public CinemachineCamera playerCam;

    [Header("Value")]
    [SerializeField] private float sensitivity = 0.07f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private Vector2 lookInput;
    private float pitch;

    private void OnEnable()
    {
        lookAction.performed += OnLook;
        lookAction.canceled += OnLook;

        SetCursor(CursorLockMode.Locked, false);

        StartCoroutine(RegisterLocalAfterOwnershipReady());
    }

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        rigid = this.GetComponent<Rigidbody>();
        brain = cameraRoot.GetComponent<CinemachineBrain>();
        playerController = this.GetComponent<PlayerController>();

        lookAction = playerInput.actions["Look"];
    }

    private IEnumerator RegisterLocalAfterOwnershipReady()
    {
        yield return new WaitUntil(() => photonView != null);
        if (!photonView.IsMine) yield break;

        yield return null;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            cameraRoot.SetActive(false);
            playerInput.enabled = false;
            return;
        }

        playerMesh.layer = LayerMask.NameToLayer("Player_Local");
        playerCam.Priority = 20;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += HandlePause;
            GameManager.Instance.OnGameResumed += HandleResumed;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnInvenOpened += HandlePause;
            UIManager.Instance.OnInvenClosed += HandleResumed;
        }
    }

    private void OnDisable()
    {
        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGamePaused -= HandlePause;
        GameManager.Instance.OnGameResumed -= HandleResumed;

        UIManager.Instance.OnInvenOpened -= HandlePause;
        UIManager.Instance.OnInvenClosed -= HandleResumed;
    }

    private void SetCursor(CursorLockMode mode, bool v)
    {
        Cursor.lockState = mode;
        Cursor.visible = v;
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }


    private void LateUpdate()
    {
        if (!photonView.IsMine) return;
        if (playerController.IsEscaped) return;
        if (GameManager.Instance.IsPaused) return;
        if (InspectManager.Instance.IsInspecting) return;
        if (GameManager.Instance.IsInteractingFocused) return;
        if (UIManager.Instance.IsOpen) return;

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        // 피치(상하) 회전 계산
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // 플레이어 본체(좌우) 회전
        Quaternion deltaRot = Quaternion.Euler(0f, mouseX, 0f);
        rigid.MoveRotation(rigid.rotation * deltaRot);
    }

    public void SetBlendCut()
    {
        brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;
    }

    public void SetBlendEaseInOut(float time)
    {
        brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
        brain.DefaultBlend.Time = time;
    }

    private void HandlePause() => SetCursor(CursorLockMode.None, true);
    private void HandleResumed() => SetCursor(CursorLockMode.Locked, false);
}
