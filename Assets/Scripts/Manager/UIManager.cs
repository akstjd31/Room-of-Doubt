using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private TextMeshProUGUI objNameText;

    public void UpdateObjectNameText(string name)
    {
        objNameText.text = $"[{name}]";
    }
}
