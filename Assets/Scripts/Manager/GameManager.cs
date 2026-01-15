using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;


public class GameManager : MonoBehaviourPunCallbacks
{
    public const float MAX_X = 1f;
    public const float MIN_X = -1f;
    public const float MAX_Y = 5f;
    public const float MIN_Y = 2f;
    public static GameManager Instance;
    public Dictionary<int, QuickSlotManager> playerQuickSlotMgrData;  // <ActorNumber, 플레이어 옵젝>
    [SerializeField] private Transform playerPrefab;
    public event Action OnGamePaused; // 일시정지 이벤트
    public event Action OnGameResumed; // 재개 이벤트
    public bool isPaused = false;

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
