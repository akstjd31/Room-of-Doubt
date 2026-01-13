using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Component")]
    private PhotonView pv;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody rigid;

    [Header("Value")]
    private Vector2 moveInput;

    private void Awake()
    {
        pv = this.GetComponent<PhotonView>();
        rigid = this.GetComponent<Rigidbody>();
        playerInput = this.GetComponent<PlayerInput>();
        
        // 리지드 세팅은 이러한데, 계산량이 많기떄문에 이 부분은 최적화때 고려해야할 사항
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        moveAction = playerInput.actions["Move"];
    }

    private void OnEnable()
    {
        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine)
            return;

        if (moveInput == Vector2.zero)
            return;

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);

        // 대각선 속도 보정
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector3 nextPos = rigid.position + dir * moveSpeed * Time.fixedDeltaTime;
        rigid.MovePosition(nextPos);
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMovePerformed;
        moveAction.canceled -= OnMoveCanceled;
    }

    public void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
}
