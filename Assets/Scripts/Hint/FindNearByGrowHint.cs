using UnityEngine;

public class FindNearByGrowHint : MonoBehaviour
{
    public GlowHintText GlowHint { get; private set; }
    private int interactableLayer;
    private int originLayer;

    private void Awake()
    {
        originLayer = this.gameObject.layer;
        interactableLayer = LayerMask.NameToLayer("Interactable");
    }

    public void SetLayer(bool isOn)
    {
        this.gameObject.layer = !isOn ? interactableLayer : originLayer;
    }

    private void OnTriggerStay(Collider other)
    {
        GlowHintText gh;
        if (GlowHint == null && other.TryGetComponent<GlowHintText>(out gh))
        {
            Debug.Log("야광 힌트 매핑 완료");
            GlowHint = gh;
        }
    }
}
