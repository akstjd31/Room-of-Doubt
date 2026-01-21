using System.Collections;
using Photon.Pun;
using UnityEngine;

public class WirePuzzle : InteractableBase
{
    [SerializeField] private Transform slotsTrf;
    [SerializeField] private WirePuzzleManager puzzleMgr;

    private void Awake()
    {
        if (slotsTrf == null) slotsTrf = this.transform.GetChild(0);
        if (puzzleMgr == null) puzzleMgr = this.GetComponentInChildren<WirePuzzleManager>(true);

        type = InteractableType.Puzzle;
    }

    protected override IEnumerator InitRoutine()
    {
        if (puzzleMgr == null) yield break;

        if (PhotonNetwork.IsMasterClient)
        {
            int seed = PhotonNetwork.ServerTimestamp ^ photonView.ViewID;

            photonView.RPC(nameof(SetupRandomPuzzleRPC), RpcTarget.AllBuffered, seed);
        }
    }

    public override void Interact(int actorNumber)
    {
        if (puzzleMgr != null && puzzleMgr.cam == null)
            puzzleMgr.cam = Camera.main;

        puzzleMgr.enabled = isInteracting;
    }

    [PunRPC]
    private void SetupRandomPuzzleRPC(int seed)
    {
        puzzleMgr.SetupRandomPuzzle(seed);
        puzzleMgr.enabled = false;
    }
}
