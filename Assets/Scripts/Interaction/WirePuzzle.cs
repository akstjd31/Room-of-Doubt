using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform portsTrf;

    private void Awake()
    {
        if (portsTrf == null) portsTrf = this.transform.GetChild(0);
    }
    public override void Interact(int actorNumber)
    {
    }

    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
