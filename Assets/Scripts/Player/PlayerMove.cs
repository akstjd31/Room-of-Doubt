using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviourPun
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody rigid;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Value")]
    [SerializeField] private float moveSpeed = 4f;
    private Vector2 moveInput;

    private void OnEnable()
    {
        if (!photonView.IsMine) return;
        
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
    }

    private void Awake()
    {
        rigid = this.GetComponent<Rigidbody>();
        playerInput = this.GetComponent<PlayerInput>();

        // 리지드 세팅은 이러한데, 계산량이 많기떄문에 이 부분은 최적화때 고려해야할 사항
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        moveAction = playerInput.actions["Move"];
    }

    private void Start()
    {
        if (!photonView.IsMine) return;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += DisableMoveAction;
            GameManager.Instance.OnGameResumed += EnableMoveAction;
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // 카메라 기준 방향 벡터
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // 카메라 기준으로 변환하기
        Vector3 dir = camRight * moveInput.x + camForward * moveInput.y;

        // 대각선 속도 보정
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        Vector3 nextPos = rigid.position + dir * moveSpeed * Time.fixedDeltaTime;
        rigid.MovePosition(nextPos);
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGamePaused -= DisableMoveAction;
        GameManager.Instance.OnGameResumed -= EnableMoveAction;
    }

    private void DisableMoveAction()
    {
        if (!photonView.IsMine) return;
        moveAction.Disable();
    }

    private void EnableMoveAction()
    {
        if (!photonView.IsMine) return;
        moveAction.Enable();
    }

    public void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
}
