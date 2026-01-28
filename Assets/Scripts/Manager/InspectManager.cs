using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 아이템 자세히 보기를 위한 매니저 클래스
/// </summary>
public class InspectManager : MonoBehaviour
{
    public static InspectManager Instance;
    [SerializeField] private Transform pivot;           // 아이템이 생성될 위치
    [SerializeField] private CinemachineCamera cam;     // 포커싱될 캠
    [SerializeField] private float rotateSpeed = 0.2f;  // 아이템 잡고 마우스로 회전시킬 때 속도

    public bool IsInspecting => isInspecting;
    private bool isInspecting;

    private GameObject spawned;
    private Vector3 lastMousePos;
    private Quaternion originQut;
    private string spawnedPrefabId;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        cam.Priority = 0;
        originQut = pivot != null ? pivot.rotation : transform.rotation;
    }

    private void Update()
    {
        if (isInspecting)
            HandleRotate();
    }

    // 초기 검증 (현재 슬롯에 아이템이 있는지 부터)
    public void TryEnterFromFocusedSlot()
    {
        var slot = QuickSlotManager.Local.GetFocusedSlot();
        if (slot == null) return;
        if (slot.current == null) return;

        if (ItemManager.Instance.GetItemById(slot.current.itemId).IsLamp)
            return;

        Enter(slot);
    }

    // 자세히 보기 시작 (카메라 전환, 아이템 풀에서 꺼내기 등)
    private void Enter(Slot slot)
    {
        if (pivot == null || cam == null) return;

        pivot.rotation = originQut;

        cam.Priority = 100;
        isInspecting = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (spawned != null)
        {
            PhotonPrefabPoolManager.Instance.ReleaseLocal(spawned);
            spawned = null;
            spawnedPrefabId = null;
        }

        var item = ItemManager.Instance.GetItemById(slot.current.itemId);
        if (item == null || item.itemPrefab == null) return;


        spawnedPrefabId = $"Hints/{item.itemPrefab.name}";

        spawned = PhotonPrefabPoolManager.Instance.GetLocal(
            spawnedPrefabId,
            pivot,
            Vector3.zero,
            Quaternion.identity
        );

        if (spawned == null)
        {
            Debug.LogError($"Inspect 풀 Get 실패: {spawnedPrefabId}");
            return;
        }

        lastMousePos = Input.mousePosition;
    }

    // 자세히 보기 off (원래대로 돌려놓기)
    public void Exit()
    {
        cam.Priority = 0;
        isInspecting = false;

        // 풀 반환
        if (spawned != null)
        {
            PhotonPrefabPoolManager.Instance.ReleaseLocal(spawned);
            spawned = null;
            spawnedPrefabId = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 아이템을 집고 마구 돌리기
    private void HandleRotate()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePos = Input.mousePosition;

        if (Input.GetMouseButton(0) && spawned != null)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            pivot.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
            pivot.Rotate(Vector3.right, delta.y * rotateSpeed, Space.World);
        }
    }
}
