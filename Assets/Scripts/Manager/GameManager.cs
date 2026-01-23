using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using System.Collections.Generic;


public class GameManager : MonoBehaviourPunCallbacks
{
    // Room keys
    private const string KEY_WIRE_SEED = "PUZ_WIRE_SEED";
    private const string KEY_START_READY = "START_HINT_READY";
    private const string KEY_START_A_ID = "START_HINT_A_ID";
    private const string KEY_START_A_PAY = "START_HINT_A_PAY";
    private const string KEY_START_B_ID = "START_HINT_B_ID";
    private const string KEY_START_B_PAY = "START_HINT_B_PAY";
    private const string KEY_START_C_ID = "START_HINT_C_ID";
    private const string KEY_START_C_PAY = "START_HINT_C_PAY";
    private const string KEY_START_D_ID = "START_HINT_D_ID";
    private const string KEY_START_D_PAY = "START_HINT_D_PAY";

    [SerializeField] private SpawnPointGroup playerSpawnPointGroup;
    // Start hint 지급용
    [SerializeField] private string hintPaperItemKey; // 힌트 종이 Item GUID
    [SerializeField] private int quickSlotIndexForStartHint = 0;

    private bool startHintGivenLocal = false;
    
    public static GameManager Instance;
    private const string KEY_ROLE = "ROLE";
    public Dictionary<int, QuickSlotManager> playerQuickSlotMgrData;  // <ActorNumber, 플레이어 옵젝>
    [SerializeField] private Transform playerPrefab;
    public event Action OnGamePaused; // 일시정지 이벤트
    public event Action OnGameResumed; // 재개 이벤트
    public bool isPaused = false;
    public bool IsInteractingFocused { get; private set; }
    public bool isLocalPlayerCreated;

    void Awake()
    {
        Instance = this;
        playerQuickSlotMgrData = new Dictionary<int, QuickSlotManager>();
    }

    void Start()
    {
        StartCoroutine(SpawnPlayerWhenConnected());
        StartCoroutine(InitAfterSceneLoaded());
    }

    void Update()
    {
        // ESC == 일시정지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private IEnumerator InitAfterSceneLoaded()
    {
        // 씬 전환 직후 Photon 상태/룸/플레이어리스트/룸프로퍼티가 안정화될 때까지 한 프레임~몇 프레임 양보
        yield return null;
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    private void TrySetupStartHintsIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;

        AssignRolesIfMaster();

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        // 이미 READY면 중복 커밋 방지
        if (room.CustomProperties.TryGetValue(KEY_START_READY, out var readyObj) && (bool)readyObj)
            return;

        // 지금은 WirePuzzle 하나만 필요: seed 있어야 시작 힌트 스펙 확정 가능
        if (!room.CustomProperties.TryGetValue(KEY_WIRE_SEED, out var seedObj))
            return;

        int wireSeed = (int)seedObj;

        CommitRandomStartHints(room, wireSeed);
        Debug.Log("[GameManager] Start hint specs committed (READY=true).");
    }

    private void TryGiveLocalStartHint()
    {
        if (startHintGivenLocal) return;
        if (!PhotonNetwork.InRoom) return;
        if (!isLocalPlayerCreated) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        // READY 확인
        if (!room.CustomProperties.TryGetValue(KEY_START_READY, out var readyObj) || !(bool)readyObj)
            return;

        Debug.Log("레디 확인!");

        // 내 ROLE 확인 (Player CustomProperties)
        var lp = PhotonNetwork.LocalPlayer;
        if (lp.CustomProperties == null || !lp.CustomProperties.TryGetValue(KEY_ROLE, out var roleObj))
            return;

        string roleStr = roleObj as string;
        if (string.IsNullOrEmpty(roleStr)) return;
        char role = roleStr[0];

        // 역할에 맞는 hintId/payload 꺼내기
        if (!TryGetStartHintSpecForRole(room, role, out string hintKey, out string payload))
            return;

        Item paperItem = ItemManager.Instance.GetItemById(hintPaperItemKey);
        if (paperItem == null)
        {
            Debug.LogError($"[GameManager] HintPaper Item not found. id={hintPaperItemKey}");
            return;
        }

        // 로컬 퀵슬롯에 장착
        Debug.Log($"현재 힌트 키 : {hintKey}");
        QuickSlotManager.Instance.SetHintToSlot(quickSlotIndexForStartHint, paperItem, hintKey, payload);
        startHintGivenLocal = true;

        Debug.Log($"[GameManager] Local start hint equipped. role={role}, hintKey={hintKey}, payload={payload}");
    }

    private bool TryGetStartHintSpecForRole(Room room, char role, out string hintKey, out string payload)
    {
        hintKey = null;
        payload = null;

        string keyKey, payKey;
        switch (role)
        {
            case 'A': keyKey = KEY_START_A_ID; payKey = KEY_START_A_PAY; break;
            case 'B': keyKey = KEY_START_B_ID; payKey = KEY_START_B_PAY; break;
            case 'C': keyKey = KEY_START_C_ID; payKey = KEY_START_C_PAY; break;
            default: keyKey = KEY_START_D_ID; payKey = KEY_START_D_PAY; break;
        }

        if (!room.CustomProperties.TryGetValue(keyKey, out var keyObj)) return false;
        if (!room.CustomProperties.TryGetValue(payKey, out var payObj)) return false;

        // Room props는 object로 들어오니까 안전하게 string 캐스팅/변환
        hintKey = keyObj as string ?? keyObj?.ToString();
        payload = payObj as string ?? payObj?.ToString();

        return !string.IsNullOrEmpty(hintKey);
    }


    private void AssignRolesIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        char[] roles = { 'A', 'B', 'C', 'D' };

        for (int i = 0; i < players.Length && i < 4; i++)
        {
            var p = players[i];

            // 이미 ROLE 있으면 스킵(재호출 안전)
            if (p.CustomProperties != null && p.CustomProperties.ContainsKey(KEY_ROLE))
                continue;

            p.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { KEY_ROLE, roles[i].ToString() }
        });
        }
    }

    private void CommitRandomStartHints(Room room, int wireSeed)
{
    var players = PhotonNetwork.PlayerList;
    System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

    var rand = new System.Random(wireSeed);

    var props = new PhotonHashtable
    {
        { KEY_START_READY, true }
    };

    for (int i = 0; i < players.Length && i < 4; i++)
    {
        char role = (char)('A' + i);

        string pickedHintKey = PickRandomWireHintKey(rand);
        string payload = wireSeed.ToString();

        switch (role)
        {
            case 'A':
                props[KEY_START_A_ID] = pickedHintKey;
                props[KEY_START_A_PAY] = payload;
                break;
            case 'B':
                props[KEY_START_B_ID] = pickedHintKey;
                props[KEY_START_B_PAY] = payload;
                break;
            case 'C':
                props[KEY_START_C_ID] = pickedHintKey;
                props[KEY_START_C_PAY] = payload;
                break;
            case 'D':
                props[KEY_START_D_ID] = pickedHintKey;
                props[KEY_START_D_PAY] = payload;
                break;
        }
    }

    room.SetCustomProperties(props);
    Debug.Log("[GameManager] Random start hints committed.");
}


    // 와이어 힌트 중 하나 뽑기
    string PickRandomWireHintKey(System.Random rand)
    {
        return WireHintKeys.All[rand.Next(0, WireHintKeys.All.Length)];
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused) OnGamePaused?.Invoke(); // 구독자들에게 신호 발송
        else OnGameResumed?.Invoke();
    }

    public void OnClickResumeButton() => TogglePause();
    public void OnClickOptionsButton() => Debug.Log("제작 예정");
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
        var newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, playerSpawnPointGroup.Points[rand].position, Quaternion.identity);
        var playerPv = newPlayer.GetComponent<PhotonView>();

        // 플레이어 데이터 저장
        AddData(playerPv.Owner.ActorNumber, newPlayer.GetComponent<QuickSlotManager>());
        isLocalPlayerCreated = true;
        TryGiveLocalStartHint();
    }

    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        // seed / ready 갱신 등 이벤트로 재시도
        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        // 내 ROLE이 들어오는 순간 지급 시도
        if (targetPlayer.IsLocal && changedProps.ContainsKey(KEY_ROLE))
            TryGiveLocalStartHint();
    }


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        int actorNum = otherPlayer.ActorNumber;
        if (playerQuickSlotMgrData.ContainsKey(actorNum))
        {
            PhotonNetwork.Destroy(playerQuickSlotMgrData[actorNum].gameObject);
            RemoveData(actorNum);
        }
    }

    private void AddData(int actorNumber, QuickSlotManager quickSlot)
    {
        playerQuickSlotMgrData[actorNumber] = quickSlot;
    }

    private void RemoveData(int actorNumber)
    {
        playerQuickSlotMgrData.Remove(actorNumber);
    }
}
