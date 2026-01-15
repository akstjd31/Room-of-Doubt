using UnityEngine;
using UnityEngine.UI;

public class DragIcon : MonoBehaviour
{
    public static DragIcon Instance { get; private set; }

    [SerializeField] private RectTransform rect;
    [SerializeField] private Image image;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = true;
    }

    public void Follow(Vector2 screenPos)
    {
        rect.position = screenPos;
    }

    public void Hide()
    {
        image.enabled = false;
    }
}
