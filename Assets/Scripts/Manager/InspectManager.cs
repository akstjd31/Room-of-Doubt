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
    private bool isInspecting;                              // 현재 자세히 보기 중?

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

        // 우선순위 선정
        cam.Priority = 100;
        isInspecting = true;

        // 캠의 시작 위치 설정
        dist = distTarget = baseDist;
        cam.transform.position = pivot.position + baseDir * baseDist;
        cam.transform.LookAt(pivot);

        // 마우스 보이게끔 (오브젝트 드래그해서 확인해야하니까)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 풀에서 꺼내기
        if (spawned != null)
        {
            PhotonPrefabPoolManager.Instance.ReleaseLocal(spawned);
            spawned = null;
            spawnedPrefabId = null;
        }

        var item = ItemManager.Instance.GetItemById(slot.current.itemId);
        if (item == null || item.itemPrefab == null) return;

        spawnedPrefabId = $"Items/{item.itemPrefab.name}";

        // 로컬로 꺼내기 (자세히 보기라 굳이 동기화 할 필요 없음)
        spawned = PhotonPrefabPoolManager.Instance.GetLocal(
            spawnedPrefabId,
            pivot,
            Vector3.zero,
            Quaternion.identity
        );

        // 자식들 중에서 HintPaper 컴포넌트를 탐색 (비활성화된 자식까지 포함하려면 true 인자 추가)
        var hintPaper = spawned.GetComponentInChildren<HintPaper>(true);
        if (hintPaper != null)
        {
            // 원본 payload를 그대로 넣는 것이 아니라, Database를 통해 문구로 변환하여 전달
            string renderedText = HintDatabase.Instance.Render(slot.current.hint.hintKey, slot.current.hint.payload);
            hintPaper.SetHintText(renderedText);
        }

        if (spawned.TryGetComponent<Rigidbody>(out var rigid))
            rigid.isKinematic = true;

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

        if (spawned.TryGetComponent<Rigidbody>(out var rigid))
            rigid.isKinematic = false;

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
        // 마우스 클릭하면 클릭 자리 포지션 가져옴.
        if (Input.GetMouseButtonDown(0))
            lastMousePos = Input.mousePosition;

        // 클릭한 상태에서 움직이면 클릭 시점 포지션에서 부터 delta 계산 후 회전 적용
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

        Vector3 dir = (cam.transform.position - pivot.position).normalized;

        // 더 이상 확대가 불가능한 시점 = 오브젝트 뒤로 안넘어가게끔 방지
        if (dir.sqrMagnitude < 0.0001f)
            dir = -cam.transform.forward;

        cam.transform.position = pivot.position + dir * dist;
        cam.transform.LookAt(pivot);
    }
}
