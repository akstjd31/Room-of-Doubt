using System.Collections.Generic;
using UnityEngine;

public class WirePuzzleManager : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask portMask;
    [SerializeField] private float maxDistance = 10f;

    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;

    [Header("Answer (정답 테이블)")]
    [SerializeField] private List<Pair> answerPairs = new();

    [System.Serializable]
    public struct Pair { public int a; public int b; }

    private Dictionary<int, int> answerMap;

    // 연결 상태
    private readonly Dictionary<int, int> links = new();
    private readonly Dictionary<string, LineRenderer> lines = new();

    // 드래그 상태
    private WirePort dragFrom;
    private LineRenderer previewLine;
    private bool solved;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        answerMap = new Dictionary<int, int>();
        foreach (var p in answerPairs)
        {
            if (p.a == p.b) continue;
            answerMap[p.a] = p.b;
            answerMap[p.b] = p.a;
        }

        // 프리뷰 라인 생성
        previewLine = CreateLineRenderer("PreviewLine");
        previewLine.enabled = false;
    }

    private void Update()
    {
        if (cam == null) return;

        // 마우스 누르기 시작
        if (Input.GetMouseButtonDown(0))
        {
            var port = RaycastPort();
            if (port != null)
            {
                dragFrom = port;
                StartPreview(port);
            }
        }

        // 프리뷰 라인이 마우스를 따라다니게끔
        if (Input.GetMouseButton(0) && dragFrom != null)
        {
            UpdatePreview(dragFrom);
        }

        // 연결 or 취소
        if (Input.GetMouseButtonUp(0) && dragFrom != null)
        {
            var port = RaycastPort();
            EndPreview();

            if (port != null && port != dragFrom)
            {
                TryConnect(dragFrom, port);
            }

            dragFrom = null;
        }

        // 우클릭은 해당 프리뷰 취소
        if (Input.GetMouseButtonDown(1))
        {
            var port = RaycastPort();
            if (port != null)
            {
                DisconnectPort(port.portId);
            }

            dragFrom = null;
            EndPreview();

            CheckSolved();
        }
    }

    private WirePort RaycastPort()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, maxDistance, portMask)) return null;
        return hit.collider.GetComponentInParent<WirePort>();
    }


    // 프리뷰 라인 그리기 시작
    private void StartPreview(WirePort from)
    {
        previewLine.enabled = true;
        previewLine.positionCount = 2;
        previewLine.SetPosition(0, from.AnchorPos);
        previewLine.SetPosition(1, from.AnchorPos);
    }

    // 마우스 따라다니기 (프리뷰 갱신)
    private void UpdatePreview(WirePort from)
    {
        Vector3 world = GetMouseWorldPoint(from.AnchorPos);
        previewLine.SetPosition(0, from.AnchorPos);
        previewLine.SetPosition(1, world);
    }

    // 프리뷰 끄기
    private void EndPreview()
    {
        if (previewLine != null) previewLine.enabled = false;
    }

    private Vector3 GetMouseWorldPoint(Vector3 referencePoint)
    {
        float depth = Vector3.Dot(referencePoint - cam.transform.position, cam.transform.forward);
        depth = Mathf.Max(0.01f, depth);

        Vector3 sp = Input.mousePosition;
        sp.z = depth;
        return cam.ScreenToWorldPoint(sp);
    }

    // 연결 시도
    private void TryConnect(WirePort a, WirePort b)
    {
        int aId = a.portId;
        int bId = b.portId;

        // 이미 연결되어 있다? 연결 끊기
        if (IsDirectLinked(aId, bId))
        {
            Disconnect(aId, bId);
            CheckSolved();
            return;
        }

        // 기존 연결이 있다면 끊어버리기
        if (links.TryGetValue(aId, out int aLinked))
            Disconnect(aId, aLinked);
        if (links.TryGetValue(bId, out int bLinked))
            Disconnect(bId, bLinked);

        links[aId] = bId;
        links[bId] = aId;

        CreateOrUpdateLine(a, b);
        CheckSolved();
    }

    // a, b가 연결되어 있는지?
    private bool IsDirectLinked(int aId, int bId)
        => links.TryGetValue(aId, out int other) && other == bId;

    // 연결 끊기 (기존 링크 리스트에서 제거)
    private void Disconnect(int aId, int bId)
    {
        if (links.TryGetValue(aId, out int otherA) && otherA == bId) links.Remove(aId);
        if (links.TryGetValue(bId, out int otherB) && otherB == aId) links.Remove(bId);

        string key = MakeKey(aId, bId);
        if (lines.TryGetValue(key, out var lr) && lr != null) Destroy(lr.gameObject);
        lines.Remove(key);
    }

    // 포트 연결 끊기
    private void DisconnectPort(int portId)
    {
        if (!links.TryGetValue(portId, out int linkedId))
            return; // 연결 없음

        Disconnect(portId, linkedId);
    }

    // a, b를 연결짓는 새로운 라인 생성
    private void CreateOrUpdateLine(WirePort a, WirePort b)
    {
        string key = MakeKey(a.portId, b.portId);

        if (!lines.TryGetValue(key, out var lr) || lr == null)
        {
            lr = CreateLineRenderer($"Wire_{key}");
            lines[key] = lr;
        }

        lr.SetPosition(0, a.AnchorPos);
        lr.SetPosition(1, b.AnchorPos);
    }

    // 라인 렌더러 생성
    private LineRenderer CreateLineRenderer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        if (lineMaterial != null) lr.material = lineMaterial;

        return lr;
    }

    // a, b를 연결짓는 선 이름 정하기
    private string MakeKey(int aId, int bId)
    {
        int min = Mathf.Min(aId, bId);
        int max = Mathf.Max(aId, bId);
        return $"{min}_{max}";
    }

    // 모든 선이 정답지와 일치한다면? 해결
    private void CheckSolved()
    {
        if (solved) return;
        if (answerMap == null || answerMap.Count == 0) return;

        foreach (var kv in answerMap)
        {
            if (!links.TryGetValue(kv.Key, out int linked) || linked != kv.Value)
                return;
        }

        solved = true;
        Debug.Log("해결!");
        
        // 이후 작업 
    }
}
