using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 일단 텍스트 출력용 관리자인데 나중에 바뀔 수 있음.
public class UIManager : Singleton<UIManager>
{
    [SerializeField] private TextMeshProUGUI objNameText;

    public void UpdateObjectNameText(string name)
    {
        objNameText.text = $"[{name}]";
    }
}
