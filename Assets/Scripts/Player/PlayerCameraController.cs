using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerCameraController : MonoBehaviourPun
{
    private PlayerInput playerInput;
    private InputAction lookAction;
    private Rigidbody rigid;

    [Header("References")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform cameraPivot;

    [Header("Local Camera")]
    [SerializeField] private GameObject cameraRoot;

    [Header("Value")]
    [SerializeField] private float sensitivity = 0.07f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private Vector2 lookInput;
    private float pitch;

    private void OnEnable()
    {
        if (!photonView.IsMine)
        {
            playerInput.enabled = false;
            cameraRoot.SetActive(false);
            enabled = false;
            return;
        }

        lookAction.performed += OnLook;
        lookAction.canceled += OnLook;
        
        SetCursor(CursorLockMode.Locked, false);
    }

    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        rigid = this.GetComponent<Rigidbody>();

        lookAction = playerInput.actions["Look"];
    }

    private void Start()
    {
        if (!photonView.IsMine) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += HandlePause;
            GameManager.Instance.OnGameResumed += HandleResumed;
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
    }

    private void HandlePause() => SetCursor(CursorLockMode.Confined, true);
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
        if (GameManager.Instance.isPaused)
            return;

        // 위 OnEnable에서 걸러지지만 확실한 셒코딩
        if (!photonView.IsMine) 
            return;

        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Quaternion deltaRot = Quaternion.Euler(0f, mouseX, 0f);
        rigid.MoveRotation(rigid.rotation * deltaRot);
    }
    
}
