using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInspectController : MonoBehaviour
{
    private PlayerInput playerInput;
    [SerializeField] private Transform pivot;

    [Header("Control")]
    [SerializeField] private float rotateSpeed = 0.2f;

    public bool IsInspecting => isInspecting;
    private bool isInspecting;
    private GameObject spawned;
    private Vector3 lastMousePos;


    private void Awake()
    {
        playerInput = this.GetComponent<PlayerInput>();
        pivot = GameObject.FindWithTag("InspectPivot").transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isInspecting) Exit();
            else TryEnterFromFocusedSlot();
        }

        if (isInspecting)
        {
            HandleRotate();
        }
    }

    private void TryEnterFromFocusedSlot()
    {
        var slot = QuickSlotManager.Instance.GetFocusedSlot();
        if (slot.currentItem == null) return;
        
        Enter(slot);
    }

    private void Enter(Slot slot)
    {
        isInspecting = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (spawned != null) Destroy(spawned);

        spawned = Instantiate(slot.currentItem.itemPrefab, pivot);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        lastMousePos = Input.mousePosition;

    }

    private void Exit()
    {
        isInspecting = false;

        if (spawned != null)
        {
            Destroy(spawned);
            spawned = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleRotate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && spawned != null)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            // Yaw/Pitch 회전
            pivot.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
            pivot.Rotate(Vector3.right, delta.y * rotateSpeed, Space.World);
        }
    }
}
