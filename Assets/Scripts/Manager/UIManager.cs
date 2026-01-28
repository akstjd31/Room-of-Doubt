using UnityEngine;
using TMPro;
using System;
using System.Collections;
using Photon.Pun;
using ExitGames.Client.Photon;

public class UIManager : MonoBehaviourPunCallbacks
{
    public static UIManager Instance;

    [Header("Fade effect")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeOutTime = 1.2f;

    private Coroutine routine;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject playerAim;
    public bool IsOpen { get; private set; }
    public event Action OnInvenOpened;
    public event Action OnInvenClosed;

    // =========================
    // Time Attack UI (추가)
    // =========================
    private const string TA_START = "TA_START";

    [Header("Time Attack UI")]
    [SerializeField] private TextMeshProUGUI timeAttackText;  // 타이머 텍스트
    [SerializeField] private bool showMilliseconds = false;   // 00:00.0 형태

    private bool timeAttackStarted;
    private double timeAttackStartTime; // PhotonNetwork.Time 기준
    private bool timeAttackFinished;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += PauseMenuActivate;
            GameManager.Instance.OnGameResumed += PauseMenuDeactivate;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleUI();

        if (!timeAttackStarted || timeAttackFinished || timeAttackText == null) return;
        if (GameManager.Instance == null) return;

        // 타임어택 표시 갱신   
        double elapsed = PhotonNetwork.Time - timeAttackStartTime;
        if (elapsed < 0) elapsed = 0;

        double remain = GameManager.Instance.TimeLimitSeconds - elapsed;
        if (remain < 0) remain = 0;

        timeAttackText.text = FormatTime(remain);

        if (remain <= 0.0f && !timeAttackFinished)
        {
            timeAttackFinished = true;
            OnTimeAttackExpired();
        }

    }

    public void StartTimeAttack(double startTime)
    {
        timeAttackStartTime = startTime;
        timeAttackStarted = true;
    }

    private void OnTimeAttackExpired()
    {
        ShowMessage("시간 초과!");
        RoomRewardManager.Instance.FinalizeGoldRewards();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.TryGetValue(TA_START, out var v) && v is double t)
        {
            StartTimeAttack(t);
        }
    }


    private string FormatTime(double seconds)
    {
        int total = Mathf.FloorToInt((float)seconds);
        int mm = total / 60;
        int ss = total % 60;

        if (!showMilliseconds)
            return $"{mm:00}:{ss:00}";

        int ms1 = Mathf.FloorToInt((float)((seconds - total) * 10.0)); // 0.0~0.9
        return $"{mm:00}:{ss:00}.{ms1:0}";
    }

    void ToggleUI()
    {
        IsOpen = !IsOpen;
        SharedInventoryManager.Instance.SetPanelActive(IsOpen);
        if (IsOpen) OnInvenOpened?.Invoke();
        else OnInvenClosed?.Invoke();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGamePaused -= PauseMenuActivate;
        GameManager.Instance.OnGameResumed -= PauseMenuDeactivate;
    }

    public void ShowMessage(string message)
    {
        promptText.text = message;

        SetAlpha(1f);

        // 기존 코루틴이 시작중이라면 끊고 다시
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOutSequence());
    }

    private IEnumerator FadeOutSequence()
    {
        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / fadeOutTime);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        var c = promptText.color;
        c.a = a;
        promptText.color = c;
    }

    public void PauseMenuActivate()
    {
        SetPlayerAimActive(false);
        pauseMenu.SetActive(true);
    }

    public void PauseMenuDeactivate()
    {
        SetPlayerAimActive(true);
        pauseMenu.SetActive(false);
    }

    public void SetPlayerAimActive(bool active) => playerAim.SetActive(active);
}
