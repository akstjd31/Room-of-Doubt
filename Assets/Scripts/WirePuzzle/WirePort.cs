using UnityEngine;

[DisallowMultipleComponent]
public class WirePort : MonoBehaviour
{
    [Header("ID (정답 매칭용)")]
    public int portId;

    [Header("시각용(선택)")]
    public Transform wireAnchor;

    public Vector3 AnchorPos => (wireAnchor != null) ? wireAnchor.position : this.transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(AnchorPos, 0.03f);
    }
    
}
