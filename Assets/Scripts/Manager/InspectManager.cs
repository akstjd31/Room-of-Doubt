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

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.6f;
    [SerializeField] private float minDistance = 0.25f;   // pivot을 절대 못 넘게 (0보다 크게)
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private float zoomLerp = 12f;

    private float baseDist;
    private float dist;
    private float distTarget;
    private Vector3 baseDir;

    private Vector3 camPosOrigin;
    private Quaternion camRotOrigin;


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

    // 기본 거리/방향 저장
    if (pivot != null && cam != null)
    {
        Vector3 v = cam.transform.position - pivot.position;
        if (v.sqrMagnitude < 0.0001f) v = -cam.transform.forward;

        baseDist = v.magnitude;
        baseDir = v.normalized;

        dist = distTarget = baseDist;
    }
}




    private void Update()
    {
        // 특정 퀵 슬롯에서 아이템 드래그 중이라면
        if (UIDragState.IsDragging) return;
        
        if (!isInspecting) return;

        HandleRotate();
        HandleZoom();
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

        dist = distTarget = baseDist;
        cam.transform.position = pivot.position + baseDir * baseDist;
        cam.transform.LookAt(pivot);

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


        spawnedPrefabId = $"Items/{item.itemPrefab.name}";

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

    private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            // 휠 올리면 가까워지게(거리 감소)
            distTarget = Mathf.Clamp(distTarget - wheel * zoomSpeed, minDistance, maxDistance);
        }

        dist = Mathf.Lerp(dist, distTarget, Time.deltaTime * zoomLerp);

        // ✅ 카메라가 pivot을 바라보고 있다는 전제:
        // 카메라의 forward 방향으로 "pivot에서 뒤로" dist만큼 떨어뜨린 위치로 고정
        // (pivot -> camera 방향으로 항상 양수 dist 유지 = pivot 통과 불가)
        Vector3 dir = (cam.transform.position - pivot.position).normalized;
        if (dir.sqrMagnitude < 0.0001f)
            dir = -cam.transform.forward; // 혹시 같은 위치면 fallback

        cam.transform.position = pivot.position + dir * dist;

        // ✅ 항상 pivot을 바라보게(원하면 끄기)
        cam.transform.LookAt(pivot);
    }


}
