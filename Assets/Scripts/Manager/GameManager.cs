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

    // Start hint 지급용
    [SerializeField] private string hintPaperItemId; // 힌트 종이 Item GUID
    [SerializeField] private int quickSlotIndexForStartHint = 0;

    // 예: 힌트 템플릿 ID (나중에 HintDatabase랑 연결)
    [SerializeField] private int hintIdForRoleA = 2001; // Wire 색 매핑표
    [SerializeField] private int hintIdForRoleB = 2002; // (예) 포트 번호 매핑표
    [SerializeField] private int hintIdForRoleC = 2003;
    [SerializeField] private int hintIdForRoleD = 2004;

    private bool startHintGivenLocal = false;

    public const float MAX_X = 1f;
    public const float MIN_X = -1f;
    public const float MAX_Y = 5f;
    public const float MIN_Y = 2f;
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
    }

    void Update()
    {
        // ESC == 일시정지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
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

        // payload는 "seed"만 넣어도 됨 (읽을 때 WirePuzzleManager로 실제 힌트 생성)
        // 역할별로 다른 표현을 주고 싶으면 hintId만 다르게 두면 됨.
        var props = new PhotonHashtable
    {
        { KEY_START_A_ID, hintIdForRoleA }, { KEY_START_A_PAY, wireSeed.ToString() },
        { KEY_START_B_ID, hintIdForRoleB }, { KEY_START_B_PAY, wireSeed.ToString() },
        { KEY_START_C_ID, hintIdForRoleC }, { KEY_START_C_PAY, wireSeed.ToString() },
        { KEY_START_D_ID, hintIdForRoleD }, { KEY_START_D_PAY, wireSeed.ToString() },
        { KEY_START_READY, true }
    };

        room.SetCustomProperties(props);
        Debug.Log("[GameManager] Start hint specs committed (READY=true).");
    }

    private void TryGiveLocalStartHint()
    {
        if (startHintGivenLocal) return;
        if (!PhotonNetwork.InRoom) return;
        if (!isLocalPlayerCreated) return; // 네가 이미 만든 플래그 사용

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        // READY 확인
        if (!room.CustomProperties.TryGetValue(KEY_START_READY, out var readyObj) || !(bool)readyObj)
            return;

        // 내 ROLE 확인 (Player CustomProperties)
        var lp = PhotonNetwork.LocalPlayer;
        if (lp.CustomProperties == null || !lp.CustomProperties.TryGetValue(KEY_ROLE, out var roleObj))
            return;

        string roleStr = roleObj as string;
        if (string.IsNullOrEmpty(roleStr)) return;
        char role = roleStr[0];

        // 역할에 맞는 hintId/payload 꺼내기
        if (!TryGetStartHintSpecForRole(room, role, out int hintId, out string payload))
            return;

        Item paperItem = ItemManager.Instance.GetItemById(hintPaperItemId);
        if (paperItem == null)
        {
            Debug.LogError($"[GameManager] HintPaper Item not found. id={hintPaperItemId}");
            return;
        }

        // 로컬 퀵슬롯에 장착
        QuickSlotManager.Instance.SetHintToSlot(quickSlotIndexForStartHint, paperItem, hintId, payload);
        startHintGivenLocal = true;

        Debug.Log($"[GameManager] Local start hint equipped. role={role}, hintId={hintId}, payload={payload}");
    }

    private bool TryGetStartHintSpecForRole(Room room, char role, out int hintId, out string payload)
    {
        hintId = 0;
        payload = null;

        string idKey, payKey;
        switch (role)
        {
            case 'A': idKey = KEY_START_A_ID; payKey = KEY_START_A_PAY; break;
            case 'B': idKey = KEY_START_B_ID; payKey = KEY_START_B_PAY; break;
            case 'C': idKey = KEY_START_C_ID; payKey = KEY_START_C_PAY; break;
            default: idKey = KEY_START_D_ID; payKey = KEY_START_D_PAY; break;
        }

        if (!room.CustomProperties.TryGetValue(idKey, out var idObj)) return false;
        if (!room.CustomProperties.TryGetValue(payKey, out var payObj)) return false;

        hintId = (int)idObj;
        payload = (string)payObj;
        return true;
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
        Vector3 randPos = new Vector3
        (
            UnityEngine.Random.Range(MIN_X, MAX_X),
            UnityEngine.Random.Range(MIN_Y, MAX_Y),
            UnityEngine.Random.Range(MIN_X, MAX_X)
        );

        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        var newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, randPos, Quaternion.identity);
        var playerPv = newPlayer.GetComponent<PhotonView>();

        // 플레이어 데이터 저장
        AddData(playerPv.Owner.ActorNumber, newPlayer.GetComponent<QuickSlotManager>());
        isLocalPlayerCreated = true;
        TryGiveLocalStartHint();
    }

    public override void OnJoinedRoom()
    {
        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TrySetupStartHintsIfMaster();
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
