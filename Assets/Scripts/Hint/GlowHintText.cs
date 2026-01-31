using TMPro;
using UnityEngine;

public class GlowHintText : MonoBehaviour
{
    private int interactableLayer;
    private int originLayer;
    [SerializeField] TMP_Text text;

    [Header("Glow Color")]
    [SerializeField] Color glowColor = new Color(0.6f, 1f, 0.6f, 1f);

    [Header("Emission Power")]
    [SerializeField] float glowOn = 2.2f;
    [SerializeField] float glowOff = 0f;

    Material mat;

    private void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();

        mat = new Material(text.fontMaterial);
        text.fontMaterial = mat;
        originLayer = this.gameObject.layer;
        interactableLayer = LayerMask.NameToLayer("Interactable");
    }

    private void Start()
    {
        // 시작은 꺼진 상태
        SetVisible(false);
    }

    public void SetGlowVisible(bool isLightOn)
    {
        SetVisible(!isLightOn);

        this.gameObject.layer = isLightOn ? originLayer : interactableLayer;
    }

    public void SetText(string t) => text.text = t;

    public void SetVisible(bool visible)
    {
        text.enabled = visible;

        if (!visible)
        {
            SetGlow(glowOff);
            return;
        }

        // 바로 야광 ON
        SetGlow(glowOn);

        var c = glowColor;
        c.a = 1f;
        text.color = c;
    }

    void SetGlow(float power)
    {
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", glowColor * power);
        else if (mat.HasProperty("_GlowColor"))
            mat.SetColor("_GlowColor", glowColor * power);
    }
}
