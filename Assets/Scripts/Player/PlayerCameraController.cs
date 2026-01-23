using System;
using System.Collections;
using Photon.Pun;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerCameraController : MonoBehaviourPun
{
    public static PlayerCameraController Instance;
    private PlayerInput playerInput;
    private InputAction lookAction;
    private Rigidbody rigid;

    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform cameraPivot;

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

        lookAction = playerInput.actions["Look"];
    }

    private IEnumerator RegisterLocalAfterOwnershipReady()
    {
        // PhotonView가 생길 때까지 (보통 즉시지만 안전빵)
        yield return new WaitUntil(() => photonView != null);

        // 소유권/로컬플레이어가 안정화될 때까지 한 프레임 양보
        yield return null;

        if (!photonView.IsMine) yield break;

        Instance = this;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            cameraRoot.SetActive(false);
            return;
        }

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

    private void HandlePause() => SetCursor(CursorLockMode.None, true);
    private void HandleResumed() => SetCursor(CursorLockMode.Locked, false);

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
        if (GameManager.Instance.isPaused) return;
        if (InspectManager.Instance.IsInspecting) return;
        if (GameManager.Instance.IsInteractingFocused) return;
        if (UIManager.Instance.IsOpen) return;

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

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
}
