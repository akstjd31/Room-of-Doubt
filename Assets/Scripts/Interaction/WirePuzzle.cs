using System.Collections;
using Photon.Pun;
using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform portsTrf;
    [SerializeField] private WirePuzzleManager puzzleManager;

    private int usingActorNumber = -1;  // 현재 사용중인 플레이어

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

        // 사용중인 사람 액터넘버 연결
        if (isInteracting)
        {
            usingActorNumber = actorNumber;
        }
        else
        {
            if (usingActorNumber == actorNumber)
                usingActorNumber = -1;
        }

        bool isLocalActor = PhotonNetwork.LocalPlayer != null &&
                        PhotonNetwork.LocalPlayer.ActorNumber == actorNumber;

    if (puzzleManager != null)
        puzzleManager.enabled = isInteracting && isLocalActor;
    }


    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
