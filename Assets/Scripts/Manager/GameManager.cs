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
    [SerializeField] private SpawnPointGroup playerSpawnPointGroup;

    [Header("Start Hint 지급")]
    [SerializeField] private string lampItemId;      // 램프 Item SO GUID
    [SerializeField] private string hintPaperItemId; // 힌트 종이 Item SO GUID

    [SerializeField] private int quickSlotIndexForStartHint = 0;

    private bool startHintGivenLocal = false;

    public static GameManager Instance;

    public Dictionary<int, QuickSlotManager> playerQuickSlotMgrData;
    [SerializeField] private Transform playerPrefab;

    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public bool isPaused = false;

    public bool IsInteractingFocused { get; private set; }
    public bool isLocalPlayerCreated;

    [Header("Light")]
    [SerializeField] private GameObject[] lights;

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
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // 불 키기(로컬 적용만) - 네트워크 동기화까지 하려면 RoomPropKeys.POWER_ON 이용 권장
    public void PowerOn()
    {
        if (lights == null || lights.Length < 1) return;
        foreach (var obj in lights) obj.SetActive(true);
    }

    private IEnumerator InitAfterSceneLoaded()
    {
        yield return null;
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    private void TrySetupStartHintsIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;

        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        // 규칙: 4명 다 찼을 때만 시작 힌트 커밋
        // if (room.PlayerCount < 4) return;

        AssignRolesIfMaster();

        // 이미 READY면 중복 커밋 방지
        if (room.CustomProperties.TryGetValue(RoomPropKeys.START_READY, out var readyObj) && readyObj is bool b && b)
            return;

        // WirePuzzle seed 필요
        if (!room.CustomProperties.TryGetValue(PuzzleKeys.KEY_WIRE_SEED, out var seedObj))
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

    if (!room.CustomProperties.TryGetValue(RoomPropKeys.START_READY, out var readyObj) ||
        !(readyObj is bool ready) || !ready)
        return;

    var lp = PhotonNetwork.LocalPlayer;
    if (lp.CustomProperties == null || !lp.CustomProperties.TryGetValue(RoomPropKeys.ROLE, out var roleObj))
        return;

    string roleStr = roleObj as string;
    if (string.IsNullOrEmpty(roleStr)) return;

    char role = roleStr[0];

    // ✅ A는 램프, 나머지는 힌트
    if (role == 'A')
    {
        Item lamp = ItemManager.Instance.GetItemById(lampItemId);
        if (lamp == null)
        {
            Debug.LogError($"[GameManager] Lamp item not found. id={lampItemId}");
            return;
        }

        // 램프 지급(인벤/퀵슬롯 정책에 맞게)
        QuickSlotManager.Instance.AddItem(new ItemInstance(lamp.ID, HintData.Empty));
        startHintGivenLocal = true;
        Debug.Log("[GameManager] A got LAMP.");
        return;
    }

    // B/C/D는 힌트 종이 지급
    if (!TryGetStartHintSpecForRole(room, role, out string hintKey, out string payload))
        return;

    Item paper = ItemManager.Instance.GetItemById(hintPaperItemId);
    if (paper == null)
    {
        Debug.LogError($"[GameManager] HintPaper item not found. id={hintPaperItemId}");
        return;
    }

    QuickSlotManager.Instance.SetHintToSlot(
        quickSlotIndexForStartHint,
        paper,
        hintKey,
        payload
    );

    startHintGivenLocal = true;
    Debug.Log($"[GameManager] {role} got HintPaper. hintKey={hintKey}");
}


    private bool TryGetStartHintSpecForRole(Room room, char role, out string hintKey, out string payload)
    {
        hintKey = null;
        payload = null;

        string keyKey, payKey;
        switch (role)
        {
            case 'A': keyKey = RoomPropKeys.START_A_ID; payKey = RoomPropKeys.START_A_PAY; break;
            case 'B': keyKey = RoomPropKeys.START_B_ID; payKey = RoomPropKeys.START_B_PAY; break;
            case 'C': keyKey = RoomPropKeys.START_C_ID; payKey = RoomPropKeys.START_C_PAY; break;
            default:  keyKey = RoomPropKeys.START_D_ID; payKey = RoomPropKeys.START_D_PAY; break;
        }

        if (!room.CustomProperties.TryGetValue(keyKey, out var keyObj)) return false;
        if (!room.CustomProperties.TryGetValue(payKey, out var payObj)) return false;

        hintKey = keyObj as string ?? keyObj?.ToString();
        payload = payObj as string ?? payObj?.ToString();

        return !string.IsNullOrEmpty(hintKey);
    }

    private void AssignRolesIfMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var players = PhotonNetwork.PlayerList;
        Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        char[] roles = { 'A', 'B', 'C', 'D' };

        for (int i = 0; i < players.Length && i < 4; i++)
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

    private void CommitRandomStartHints(Room room, int wireSeed)
    {
        var players = PhotonNetwork.PlayerList;
        Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        var rand = new System.Random(wireSeed);

        var props = new PhotonHashtable
        {
            { RoomPropKeys.START_READY, true }
        };

        for (int i = 0; i < players.Length && i < 4; i++)
        {
            char role = (char)('A' + i);

            // ✅ 핵심: WireStart 풀에서만 뽑기
            string pickedHintKey = PickRandomWireStartHintKey(rand);

            // payload는 지금은 wireSeed로 통일 (필요하면 role/인덱스/서브시드 포함 가능)
            string payload = wireSeed.ToString();

            switch (role)
            {
                case 'A': props[RoomPropKeys.START_A_ID] = pickedHintKey; props[RoomPropKeys.START_A_PAY] = payload; break;
                case 'B': props[RoomPropKeys.START_B_ID] = pickedHintKey; props[RoomPropKeys.START_B_PAY] = payload; break;
                case 'C': props[RoomPropKeys.START_C_ID] = pickedHintKey; props[RoomPropKeys.START_C_PAY] = payload; break;
                case 'D': props[RoomPropKeys.START_D_ID] = pickedHintKey; props[RoomPropKeys.START_D_PAY] = payload; break;
            }
        }

        room.SetCustomProperties(props);
        Debug.Log("[GameManager] Random start hints committed.");
    }

    private string PickRandomWireStartHintKey(System.Random rand)
    {
        var pool = HintPools.WireStart;
        return pool[rand.Next(0, pool.Length)];
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused) OnGamePaused?.Invoke();
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

        AddData(playerPv.Owner.ActorNumber, newPlayer.GetComponent<QuickSlotManager>());
        isLocalPlayerCreated = true;

        TryGiveLocalStartHint();
    }

    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        TrySetupStartHintsIfMaster();
        TryGiveLocalStartHint();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        if (targetPlayer.IsLocal && changedProps.ContainsKey(RoomPropKeys.ROLE))
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
