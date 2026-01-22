using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class WirePuzzleManager : MonoBehaviourPunCallbacks
{

    [Header("Raycast")]
    [SerializeField] public Camera cam;
    [SerializeField] private LayerMask portMask;
    [SerializeField] private float maxDistance = 30f;

    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.02f;

    [Header("Answer (정답지)")]
    [SerializeField] private List<Pair> answerPairs = new();

    [System.Serializable]
    public struct Pair { public int a; public int b; }

    [Header("Port Colors")]
    [SerializeField] private Material[] colorMaterials;

    [Header("Color Names (배치된 포트 기준)")]
    [SerializeField] private string[] colorNames;

    private readonly Dictionary<int, WirePort> portById = new();

    private Dictionary<int, int> answerMap;

    // 연결 상태
    private readonly Dictionary<int, int> links = new();
    private Dictionary<int, int> reservations = new();
    private readonly Dictionary<string, LineRenderer> lines = new();

    // 드래그 상태
    private WirePort dragFrom;
    private LineRenderer previewLine;
    private bool solved;

    [Header("Random Setup")]
    [SerializeField] private Transform topSlotsParent;
    [SerializeField] private Transform bottomSlotsParent;
    [SerializeField] private WirePort portPrefab;
    [SerializeField] private int columns = 6;
    [SerializeField] private int pairCount = 3;

    [SerializeField] private Transform portParent;
    private readonly List<WirePort> spawnedPorts = new();

    private int topCount => columns;

    private bool IsTop(int id) => id >= 1 && id <= topCount;
    private bool IsBottom(int id) => id > topCount && id <= topCount * 2;

    // 탑 - 바텀 쌍이어야 함.
    private bool IsValidPair(int aId, int bId)
    {
        return (IsTop(aId) && IsBottom(bId)) || (IsBottom(aId) && IsTop(bId));
    }

    private void Awake()
    {
        answerMap = new Dictionary<int, int>();
        colorNames = new string[columns * 2];
        // foreach (var p in answerPairs)
        // {
        //     if (p.a == p.b) continue;
        //     answerMap[p.a] = p.b;
        //     answerMap[p.b] = p.a;
        // }

        // 프리뷰 라인 생성
        previewLine = CreateLineRenderer("PreviewLine");
    }

    public void SetupRandomPuzzle(int seed)
    {
        ClearAllWires();
        ClearSpawnedPorts();

        answerPairs.Clear();
        portById.Clear();

        int n = columns;

        // 각 슬롯 자식 리스트 가져오기
        var topSlots = GetChildSlots(topSlotsParent);
        var bottomSlots = GetChildSlots(bottomSlotsParent);

        if (topSlots.Count < n || bottomSlots.Count < n)
        {
            Debug.LogError("슬롯 부족!");
        }

        // -1이면 랜덤, 아니면 고정
        var rand = new System.Random(seed);

        Shuffle(topSlots, rand);
        Shuffle(bottomSlots, rand);

        // 셔플된 리스트에 생성 및 각 ID 부여
        for (int i = 0; i < n; i++)
        {
            var p = Instantiate(portPrefab, topSlots[i].position, topSlots[i].rotation, portParent);
            p.portId = i + 1;
            spawnedPorts.Add(p);
            portById[p.portId] = p;
        }

        for (int i = 0; i < n; i++)
        {
            var p = Instantiate(portPrefab, bottomSlots[i].position, bottomSlots[i].rotation, portParent);
            p.portId = n + i + 1;
            spawnedPorts.Add(p);
            portById[p.portId] = p;
        }

        // 색 배정 (턉)
        var topColorIdx = new List<int>();
        for (int i = 0; i < n; i++)
            topColorIdx.Add(i);
        Shuffle(topColorIdx, rand);

        // 색 배정 (바텀)
        var bottomColorIdx = new List<int>();
        for (int i = 0; i < n; i++)
            bottomColorIdx.Add(i);
        Shuffle(bottomColorIdx, rand);

        // 색 적용 (탑)
        var topColorToPortId = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            int portId = i + 1;
            int cIdx = topColorIdx[i];
            SetPortMaterial(portId, colorMaterials[cIdx]);
            colorNames[i] = colorMaterials[cIdx].name;
            topColorToPortId[cIdx] = portId;
        }

        // 색 적용 (바텀)
        var bottomColorToPortId = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            int portId = n + i + 1;
            int cIdx = bottomColorIdx[i];
            SetPortMaterial(portId, colorMaterials[cIdx]);
            colorNames[n + i] = colorMaterials[cIdx].name;
            bottomColorToPortId[cIdx] = portId;
        }

        var bottomColorIdxPerm = new List<int>();
        for (int i = 0; i < n; i++) bottomColorIdxPerm.Add(i);
        Shuffle(bottomColorIdxPerm, rand);

        for (int c = 0; c < n; c++)
        {
            int topPortId = topColorToPortId[c];
            int mappedBottomColor = bottomColorIdxPerm[c];
            int bottomPortId = bottomColorToPortId[mappedBottomColor];

            answerPairs.Add(new Pair { a = topPortId, b = bottomPortId });
        }

        answerMap.Clear();
        foreach (var p in answerPairs)
        {
            answerMap[p.a] = p.b;
            answerMap[p.b] = p.a;
        }

        solved = false;
        this.enabled = false;

        Debug.Log("랜덤 퍼즐 생성 완료!");
    }

    private List<Transform> GetChildSlots(Transform parent)
    {
        var result = new List<Transform>();
        if (parent == null) return result;

        foreach (Transform t in parent)
            result.Add(t);
        return result;
    }


    private void Update()
    {
        if (cam == null) return;
        if (!GameManager.Instance.IsInteractingFocused)
        {
            if (previewLine.enabled) EndPreview();
            return;
        }

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
                RequestDisconnect(port.portId);
            }

            dragFrom = null;
            EndPreview();

            CheckSolved();
        }
    }

    public string BuildColorHintText()
    {
        if (colorNames == null || colorNames.Length == 0)
            return "색 이름이 설정되지 않았습니다.";

        if (answerPairs == null || answerPairs.Count == 0)
            return null;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("회로 색상 매핑표");
        foreach (var p in answerPairs)
        {
            string topColor = colorNames[p.a - 1];
            string bottomColor = colorNames[p.b - 1];
            sb.AppendLine($"{topColor} -> {bottomColor}");
        }

        return sb.ToString();
    }

    public string BuildPortPairHintText()
    {
        if (answerPairs == null || answerPairs.Count == 0)
            return "정답 데이터가 없습니다.";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("포트 번호 매핑표");
        foreach (var p in answerPairs)
            sb.AppendLine($"{p.a} -> {p.b}");

        return sb.ToString();
    }

    // 일부만 공개(예: 처음 N개만)
    public string BuildPartialHintText(int revealCount)
    {
        if (answerPairs == null || answerPairs.Count == 0)
            return "정답 데이터가 없습니다.";

        revealCount = Mathf.Clamp(revealCount, 1, answerPairs.Count);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"부분 힌트 ({revealCount}/{answerPairs.Count})");
        for (int i = 0; i < revealCount; i++)
        {
            var p = answerPairs[i];
            sb.AppendLine($"{p.a} -> {p.b}");
        }
        sb.AppendLine("나머지는 직접 추리하세요.");

        return sb.ToString();
    }



    private void SetPortMaterial(int portId, Material mat)
    {
        if (!portById.TryGetValue(portId, out var port) || port == null) return;

        var r = port.GetComponentInChildren<Renderer>();
        if (r == null) return;

        r.material = mat;
    }


    // 셔플
    private void Shuffle<T>(List<T> list, System.Random rand)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void ClearSpawnedPorts()
    {
        for (int i = 0; i < spawnedPorts.Count; i++)
        {
            if (spawnedPorts[i] != null)
                Destroy(spawnedPorts[i].gameObject);
        }

        spawnedPorts.Clear();
    }

    private void ClearAllWires()
    {
        var keys = new List<string>(lines.Keys);
        foreach (var k in keys)
        {
            if (lines[k] != null)
                Destroy(lines[k].gameObject);
        }

        lines.Clear();
        links.Clear();
        if (previewLine != null)
            previewLine.enabled = false;
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
        if (!GameManager.Instance.IsInteractingFocused) return;

        int aId = a.portId;
        int bId = b.portId;

        // 올바른 연결이 아닌 경우 패스 (기본적으로 탑 - 바텀 쌍)
        if (!IsValidPair(aId, bId)) return;

        // 이미 연결되어 있다? 연결 끊기
        if (IsDirectLinked(aId, bId))
        {
            RequestDisconnect(aId);
            CheckSolved();
            return;
        }

        // 기존 연결이 있다면 끊어버리기
        if (links.TryGetValue(aId, out int aLinked))
            RequestDisconnect(aId);
        if (links.TryGetValue(bId, out int bLinked))
            RequestDisconnect(bId);

        RequestConnect(aId, bId);
    }

    public void RequestConnect(int aId, int bId)
    {
        photonView.RPC(nameof(RequestConnectRPC), RpcTarget.MasterClient, aId, bId, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RequestConnectRPC(int aId, int bId, int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender == null || info.Sender.ActorNumber != actorNumber) return;
        if (!IsValidPair(aId, bId)) return;

        photonView.RPC(nameof(ApplyConnectRPC), RpcTarget.AllBuffered, aId, bId, actorNumber);
    }

    [PunRPC]
    private void ApplyConnectRPC(int aId, int bId, int actorNumber)
    {
        links[aId] = bId;
        links[bId] = aId;

        CreateOrUpdateLine(portById[aId], portById[bId]);
        CheckSolved();
    }

    public void RequestDisconnect(int aId)
    {
        photonView.RPC(nameof(RequestDisconnectRPC), RpcTarget.MasterClient, aId, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RequestDisconnectRPC(int aId, int actorNumber, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender == null || info.Sender.ActorNumber != actorNumber) return;

        if (!links.TryGetValue(aId, out var bId) || bId == -1) return;
        photonView.RPC(nameof(ApplyDisconnectRPC), RpcTarget.AllBuffered, aId, bId, actorNumber);
    }

    [PunRPC]
    private void ApplyDisconnectRPC(int aId, int bId, int actorNumber)
    {
        Disconnect(aId, bId);
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

    // // 포트 연결 끊기
    // private void DisconnectPort(int portId)
    // {
    //     if (!links.TryGetValue(portId, out int linkedId))
    //         return; // 연결 없음

    //     Disconnect(portId, linkedId);
    // }

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
