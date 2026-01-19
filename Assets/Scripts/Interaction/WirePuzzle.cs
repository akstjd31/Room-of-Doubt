using System.Collections;
using Photon.Pun;
using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform portsTrf;
    [SerializeField] private WirePuzzleManager puzzleManager;

    private void Awake()
    {
        if (portsTrf == null) portsTrf = this.transform.GetChild(0);
        if (puzzleManager == null) puzzleManager = GetComponentInChildren<WirePuzzleManager>(true);

        type = InteractableType.Puzzle;
    }

    public override void Interact(int actorNumber)
    {
        if (puzzleManager != null && puzzleManager.cam == null)
            puzzleManager.cam = Camera.main;

        puzzleManager.enabled = isInteracting;
    }


    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
