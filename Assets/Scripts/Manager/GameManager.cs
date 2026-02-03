using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static GameManager Instance;

    [SerializeField] private SpawnPointGroup playerSpawnPointGroup; // 플레이어 스폰 포인트 지정

    [Header("Start Hint 지급")]
    [SerializeField] private string lampItemId;                     // 램프 Item SO GUID
    [SerializeField] private string hintPaperItemId;                // 힌트 종이 Item SO GUID

    [SerializeField] private int quickSlotIndexForStartHint = 0;    // 무조건 0번째에 지급

    private bool startHintGivenLocal = false;                       // 힌트를 주었는지?

    [SerializeField] private QuickSlotManager localQuickSlotMgr;    // 로컬 퀵 슬롯 (반드시 연결)

    // actorNumber -> packed quickslot snapshot
    private readonly Dictionary<int, string[]> quickSlotSnapshotByActor = new();
    [SerializeField] private Transform playerPrefab;

    public event Action OnGamePaused;
    public event Action OnGameResumed;
    [SerializeField] private int timeLimitSeconds = 180;
    public int TimeLimitSeconds => timeLimitSeconds;
    public bool IsPaused { get; private set; }
    public bool OptionOn { get; set; } = true;

    public bool IsInteractingFocused { get; private set; }
    private bool isLocalPlayerCreated;

    [Header("Light")]
    [SerializeField] private GameObject[] lights;
    public bool WirePuzzleSolved { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    void Start()
    {
        StartCoroutine(SpawnPlayerWhenConnected());
        StartCoroutine(InitAfterSceneLoaded());
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != QuickSlotNet.EVT_QUICKSLOT_SNAPSHOT) return;

        int senderActor = photonEvent.Sender;
        var snapshot = photonEvent.CustomData as string[];
        if (snapshot == null) return;

        Debug.Log($"[QS RECV] sender={senderActor}, isMaster={PhotonNetwork.IsMasterClient}, len={snapshot.Length}, mod3={snapshot.Length % 3}");

        if (PhotonNetwork.IsMasterClient)
            quickSlotSnapshotByActor[senderActor] = snapshot;
    }
    void Update()
    {
        if (InspectManager.Instance.IsInspecting) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void MoveAllToLobby()
    {
        StartCoroutine(MoveAllToLobbyCor());
    }

    private IEnumerator MoveAllToLobbyCor()
    {
        yield return new WaitForSeconds(1.0f);

        if (!PhotonNetwork.IsMasterClient) yield break;

        Debug.Log("방 종료 -> 전원 LeaveRoom 요청");
        photonView.RPC(nameof(LeaveRoomRPC), RpcTarget.All);
    }

    [PunRPC]
    private void LeaveRoomRPC()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    // 특정 퍼즐 해결 시 방에 있는 불 키기
    public void PowerOn()
    {
        WirePuzzleSolved = true;
        if (lights == null || lights.Length < 1) return;
        foreach (var obj in lights) obj.SetActive(true);

        SoundManager.Instance.PlayLightOnSound();
    }

    private IEnumerator InitAfterSceneLoaded()
    {
        yield return null;
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();

        yield return new WaitUntil(() => KeyPadManager.AllKeypads.Count > 0);
        if (PhotonNetwork.IsMasterClient)
        {
            SyncAllHintPapers();
        }
    }

    private void TrySetupStartHintsIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        AssignRolesIfMaster();

        // 이미 READY면 중복 커밋 방지
        if (room.CustomProperties.TryGetValue(RoomPropKeys.START_READY, out var readyObj)
            && readyObj is bool b && b)
            return;

        // 퍼즐 시드 수집
        if (!TryCollectPuzzleSeeds(room, out var seeds))
            return;

        CommitRandomStartHints(room, seeds);
        Debug.Log("힌트 뿌림 완료.");
    }

    private bool TryCollectPuzzleSeeds(Room room, out Dictionary<string, int> seeds)
    {
        seeds = new Dictionary<string, int>();

        // 퍼즐 시드 추가
        bool ok = true;
        ok &= TryAddSeed(room, PuzzleKeys.KEY_WIRE_SEED, seeds);
        ok &= TryAddSeed(room, PuzzleKeys.KEYPAD_SEED, seeds);

        return ok;
    }

    private bool TryAddSeed(Room room, string key, Dictionary<string, int> dict)
    {
        if (!room.CustomProperties.TryGetValue(key, out var obj))
            return false;

        // Photon 커스텀 프로퍼티는 int로 들어오지만 혹시 몰라 방어적으로 처리
        int seed;
        if (obj is int i) seed = i;
        else if (obj is long l) seed = unchecked((int)l);
        else if (!int.TryParse(obj.ToString(), out seed))
            return false;

        dict[key] = seed;
        return true;
    }

    private void AssignRolesIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var players = PhotonNetwork.PlayerList;
        Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        char[] roles = { 'A', 'B', 'C' };

        for (int i = 0; i < players.Length && i < 3; i++)
        {
            var p = players[i];

            // 이미 ROLE 있으면 스킵
            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(RoomPropKeys.ROLE))
                continue;

            p.SetCustomProperties(new PhotonHashtable
            {
                { RoomPropKeys.ROLE, roles[i].ToString() }
            });
        }
    }

    private void CommitRandomStartHints(Room room, Dictionary<string, int> puzzleSeeds)
{
    var players = PhotonNetwork.PlayerList;
    Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

    // 시드 생성
    int combinedSeed = 17;
    foreach (var kv in puzzleSeeds)
    {
        unchecked { combinedSeed = combinedSeed * 31 + kv.Key.GetHashCode() + kv.Value; }
    }
    var rand = new System.Random(combinedSeed);

    // 힌트 풀 섞기
    var shuffledHints = new List<string>(HintPools.Start);
    for (int i = shuffledHints.Count - 1; i > 0; i--)
    {
        int r = rand.Next(0, i + 1);
        (shuffledHints[i], shuffledHints[r]) = (shuffledHints[r], shuffledHints[i]);
    }

    // 램프 오너 정하기 (접속한 플레이어 중 랜덤)
    int lampOwnerIndex = rand.Next(0, players.Length);
    int hintPointer = 0; // 힌트 풀에서 꺼낼 인덱스

    var props = new PhotonHashtable { { RoomPropKeys.START_READY, true } };

    for (int i = 0; i < players.Length && i < 4; i++)
    {
        bool isLampOwner = (i == lampOwnerIndex);
        string hintKey = "";
        
        if (!isLampOwner)
        {
            if (hintPointer < shuffledHints.Count)
            {
                hintKey = shuffledHints[hintPointer];
                hintPointer++;
            }
        }
        else
        {
            hintKey = "";
        }

        string payload = BuildSeedPayload(puzzleSeeds);

        // 역할별 할당 (A, B, C...)
        string idKey = i == 0 ? RoomPropKeys.START_A_ID : (i == 1 ? RoomPropKeys.START_B_ID : RoomPropKeys.START_C_ID);
        string payKey = i == 0 ? RoomPropKeys.START_A_PAY : (i == 1 ? RoomPropKeys.START_B_PAY : RoomPropKeys.START_C_PAY);
        string lampKey = i == 0 ? RoomPropKeys.START_A_LAMP : (i == 1 ? RoomPropKeys.START_B_LAMP : RoomPropKeys.START_C_LAMP);

        props[idKey] = hintKey;
        props[payKey] = payload;
        props[lampKey] = isLampOwner;
    }

    room.SetCustomProperties(props);
}

    private void TryGiveLocalStartHint()
    {
        if (startHintGivenLocal || !PhotonNetwork.InRoom || !isLocalPlayerCreated) return;

        var room = PhotonNetwork.CurrentRoom;
        if (!room.CustomProperties.TryGetValue(RoomPropKeys.START_READY, out var ready) || !(bool)ready) return;

        var lp = PhotonNetwork.LocalPlayer;
        if (!lp.CustomProperties.TryGetValue(RoomPropKeys.ROLE, out var roleObj)) return;

        char role = roleObj.ToString()[0];
        string hintKey = ""; string payload = ""; bool hasLamp = false;

        // 역할별 데이터 매칭
        switch (role)
        {
            case 'A':
                hintKey = room.CustomProperties[RoomPropKeys.START_A_ID] as string;
                payload = room.CustomProperties[RoomPropKeys.START_A_PAY] as string;
                hasLamp = GetPropBool(room, RoomPropKeys.START_A_LAMP); break;
            case 'B':
                hintKey = room.CustomProperties[RoomPropKeys.START_B_ID] as string;
                payload = room.CustomProperties[RoomPropKeys.START_B_PAY] as string;
                hasLamp = GetPropBool(room, RoomPropKeys.START_B_LAMP); break;
            case 'C':
                hintKey = room.CustomProperties[RoomPropKeys.START_C_ID] as string;
                payload = room.CustomProperties[RoomPropKeys.START_C_PAY] as string;
                hasLamp = GetPropBool(room, RoomPropKeys.START_C_LAMP); break;
            // case 'D':
            //     hintKey = room.CustomProperties[RoomPropKeys.START_D_ID] as string;
            //     payload = room.CustomProperties[RoomPropKeys.START_D_PAY] as string;
            //     hasLamp = GetPropBool(room, RoomPropKeys.START_D_LAMP); break;
        }

        // 램프 지급 (1명만)
        if (hasLamp)
        {
            Item lamp = ItemManager.Instance.GetItemById(lampItemId);
            if (lamp != null) QuickSlotManager.Local.AddItem(new ItemInstance(lamp.ID, HintData.Empty));
            Debug.Log($"[Game] I am the Lamp Owner! ({role})");
        }
        else
        {
            // 힌트 종이 지급 (나머지)
            Item paper = ItemManager.Instance.GetItemById(hintPaperItemId);
            if (paper != null && !string.IsNullOrEmpty(hintKey))
            {
                QuickSlotManager.Local.SetHintToSlot(quickSlotIndexForStartHint, paper, hintKey, payload);

            }
        }
        startHintGivenLocal = true;
    }

    // GameManager.cs 내부에 추가

    public void SyncAllHintPapers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // 씬에 있는 모든 HintPaper를 찾습니다.
        HintPaper[] papers = FindObjectsByType<HintPaper>(FindObjectsSortMode.None);
        var finalKeypad = KeyPadManager.AllKeypads.Find(k => k.IsFinal);

        if (finalKeypad == null) return;

        foreach (var paper in papers)
        {
            if (finalKeypad.TryGetNextHint(out int pos, out char val))
            {
                // GameManager의 PhotonView를 사용하여 RPC를 쏩니다.
                // 종이의 이름(gameObject.name)과 뽑힌 숫자를 보냅니다.
                string answer = $"POS={pos}|VAL={val}";
                photonView.RPC(nameof(SyncSinglePaperRPC), RpcTarget.AllBuffered, paper.gameObject.name, HintDatabase.Instance.ParseDigitPayload(answer));
            }
        }

        // 이건 야광 힌트 
        GlowHintText[] glowHints = FindObjectsByType<GlowHintText>(FindObjectsSortMode.None);

        foreach (var gh in glowHints)
        {
            if (finalKeypad.TryGetNextHint(out int pos, out char val))
                photonView.RPC(nameof(SyncSingleGlowHintRPC), RpcTarget.AllBuffered, gh.gameObject.name, $"POS={pos}|VAL={val}");
        }
    }

    [PunRPC]
    private void SyncSinglePaperRPC(string paperName, string val)
    {
        var papers = Resources.FindObjectsOfTypeAll<HintPaper>();

        foreach (var paper in papers)
        {
            if (paper.gameObject.name.Equals(paperName))
            {
                paper.SetHintText(val);
            }
        }
    }

    [PunRPC]
    private void SyncSingleGlowHintRPC(string glowHintName, string val)
    {
        var glowHints = Resources.FindObjectsOfTypeAll<GlowHintText>();

        foreach (var gh in glowHints)
        {
            if (gh.gameObject.name.Equals(glowHintName))
            {
                gh.SetText(val);
                return;
            }
        }
    }



    // bool값 가져오기
    private bool GetPropBool(Room room, string key)
    {
        if (room.CustomProperties.TryGetValue(key, out var obj) && obj is bool b) return b;
        return false;
    }

    private string BuildSeedPayload(Dictionary<string, int> seeds)
    {
        // 키 순서 고정
        var keys = new List<string>(seeds.Keys);
        keys.Sort(StringComparer.Ordinal);

        var parts = new List<string>(keys.Count);
        foreach (var k in keys)
            parts.Add($"{k}={seeds[k]}");

        return string.Join("|", parts);
    }

    void TogglePause()
    {
        if (OptionOn)
        {
            OptionOn = false;
            UIManager.Instance.SetOptionPanelActive(OptionOn);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            return;
        }

        IsPaused = !IsPaused;
        if (IsPaused) OnGamePaused?.Invoke();
        else
        {
            OnGameResumed?.Invoke();

            if (UIManager.Instance.IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void OnClickResumeButton() => TogglePause();
    public void OnClickOptionsButton()
    {
        OptionOn = true;
        UIManager.Instance.SetOptionPanelActive(OptionOn);
    }

    public void OnClickQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void EnterInteracting() => IsInteractingFocused = true;
    public void ExitInteracting() => IsInteractingFocused = false;

    IEnumerator SpawnPlayerWhenConnected()
    {
        if (playerSpawnPointGroup == null)
        {
            Debug.LogError("스폰 포인트 그룹이 없음!");
            yield break;
        }

        int rand = UnityEngine.Random.Range(0, playerSpawnPointGroup.Points.Length);

        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        var newPlayer = PhotonNetwork.Instantiate(
            playerPrefab.name,
            playerSpawnPointGroup.Points[rand].position,
            Quaternion.identity
        );

        var playerPv = newPlayer.GetComponent<PhotonView>();

        if (playerPv.IsMine)
        {
            if (localQuickSlotMgr == null)
            {
                Debug.LogError("localQuickSlotMgr가 할당되지 않았음! (씬에 배치 후 연결 필요)");
            }
            else
            {
                // 로컬 퀵 슬롯 등록
                int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
                localQuickSlotMgr.AssignOwner(myActor);
                Debug.Log("로컬 퀵슬롯 추가됨!");
            }

            isLocalPlayerCreated = true;
            TryGiveLocalStartHint();
        }
    }

    // 방 커스텀 프로퍼티가 변경되었을 때
    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    // 특정 플레이어의 커스텀 프로퍼티가 변경되었을 떄
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        if (targetPlayer.IsLocal && changedProps.ContainsKey(RoomPropKeys.ROLE))
            TryGiveLocalStartHint();
    }

    // 특정 플레이어가 나갔을 때 (남아있는 플레이어들한테서 호출)
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int actorNum = otherPlayer.ActorNumber;

        // 마스터 캐시에서 찾아보기
        if (quickSlotSnapshotByActor.TryGetValue(actorNum, out var snapshot))
        {
            SharedInventoryManager.Instance.AbsorbPackedQuickSlots(snapshot);
            quickSlotSnapshotByActor.Remove(actorNum);
            return;
        }

        if (otherPlayer.CustomProperties.TryGetValue("QS", out var obj) && obj is string joined && !string.IsNullOrEmpty(joined))
        {
            var recovered = joined.Split('|');
            SharedInventoryManager.Instance.AbsorbPackedQuickSlots(recovered);

            // 혹시 모르니 캐시 제거
            quickSlotSnapshotByActor.Remove(actorNum);
            return;
        }

        Debug.LogWarning($"[LeftRoom] actor={actorNum} snapshot not found (cache+props).");
    }


    // 마스터 클라이언트 변경 시 (기존 마스터가 연결이 끊겼을 때)
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 새 마스터에게 스냅샷 전송
        QuickSlotManager.Local?.NotifySnapshotToMaster();

        if (!PhotonNetwork.IsMasterClient) return;

        quickSlotSnapshotByActor.Clear();

        // 새 마스터 클라가 모든 플레이어 상태를 복구
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("QS", out var obj) && obj is string joined && !string.IsNullOrEmpty(joined))
            {
                quickSlotSnapshotByActor[p.ActorNumber] = joined.Split('|');
            }
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public QuickSlotManager LocalQuickSlot => localQuickSlotMgr;
}
