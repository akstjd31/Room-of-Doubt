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

    protected override IEnumerator InitRoutine()
    {
        if (puzzleManager == null) yield break;

        if (PhotonNetwork.IsMasterClient)
        {
            int seed = PhotonNetwork.ServerTimestamp ^ photonView.ViewID;

            photonView.RPC(nameof(SetupRandomPuzzleRPC), RpcTarget.AllBuffered, seed);
        }
    }

    public override void Interact(int actorNumber)
    {
        if (puzzleManager != null && puzzleManager.cam == null)
            puzzleManager.cam = Camera.main;

        puzzleManager.enabled = isInteracting;
    }

    [PunRPC]
    private void SetupRandomPuzzleRPC(int seed)
    {
        puzzleManager.SetupRandomPuzzle(seed);
        puzzleManager.enabled = false;
    }


    public Transform GetTopSlotParent() => portsTrf.GetChild(0);
    public Transform GetBottomSlotParent() => portsTrf.GetChild(1);
}
