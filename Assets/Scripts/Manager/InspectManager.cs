using UnityEngine;
using Unity.Cinemachine;

public class InspectManager : Singleton<InspectManager>
{
    [SerializeField] private Transform pivot;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private float rotateSpeed = 0.2f;
    public bool IsInspecting => isInspecting;
    private bool isInspecting;
    private GameObject spawned;
    private Vector3 lastMousePos;
    private Quaternion originQut;

    private void Start()
    {
        cam.Priority = 0;
        originQut = this.transform.rotation;
    }

    private void Update()
    {
        if (isInspecting)
        {
            HandleRotate();
        }
    }

    public void TryEnterFromFocusedSlot()
    {
        var slot = QuickSlotManager.Instance.GetFocusedSlot();
        if (slot == null) return;
        if (slot.currentItem == null) return;
        
        Enter(slot);
    }

    private void Enter(Slot slot)
    {
        pivot.transform.rotation = originQut;
        cam.Priority = 100;
        isInspecting = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (spawned != null) Destroy(spawned);

        spawned = Instantiate(slot.currentItem.itemPrefab, pivot);
        spawned.transform.localRotation = pivot.transform.rotation;

        lastMousePos = Input.mousePosition;
    }

    public void Exit()
    {
        cam.Priority = 0;
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
