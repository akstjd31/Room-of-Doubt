using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

// 일단 텍스트 출력용 관리자인데 나중에 바뀔 수 있음.
public class UIManager : Singleton<UIManager>
{
    [Header("Fade effect")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private float fadeOutTime = 1.2f;  // 천천히 사라지는 시간

    private Coroutine routine;

    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject playerAim;
    public bool IsOpen { get; private set; }
    public event Action OnInvenOpened;
    public event Action OnInvenClosed;

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
