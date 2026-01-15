using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

// 일단 텍스트 출력용 관리자인데 나중에 바뀔 수 있음.
public class UIManager : Singleton<UIManager>
{
    [SerializeField] private TextMeshProUGUI objNameText;
    [SerializeField] private GameObject pauseMenu;
    public bool IsOpen { get; private set; }
    public event Action OnUIOpened;
    public event Action OnUIClosed;

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
        InventoryManager.Instance.SetPanelActive(IsOpen);
        if (IsOpen) OnUIOpened?.Invoke();
        else OnUIClosed?.Invoke();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGamePaused -= PauseMenuActivate;
        GameManager.Instance.OnGameResumed -= PauseMenuDeactivate;
    }

    public void UpdateObjectNameText(string name)
    {
        objNameText.text = $"[{name}]";
    }

    public void PauseMenuActivate() => pauseMenu.SetActive(true);
    public void PauseMenuDeactivate() => pauseMenu.SetActive(false);
}
