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
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (PhotonNetwork.IsMasterClient)
        {
            var room = PhotonNetwork.CurrentRoom;

            
            int seed = PhotonNetwork.ServerTimestamp ^ photonView.ViewID;
            if (room.CustomProperties.TryGetValue(PuzzleKeys.KEY_WIRE_SEED, out var seedObj))
                seed = (int)seedObj;
            else
            {
                seed = PhotonNetwork.ServerTimestamp ^ photonView.ViewID;

                room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { PuzzleKeys.KEY_WIRE_SEED, seed }
                });
            }

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
