using System.Collections;
using Photon.Pun;
using UnityEngine;

public static class WireHintKeys
{
    public const string COLOR_MAP = "WIRE_COLOR_MAP";   // 색 → 색 힌트
    public const string PORT_MAP  = "WIRE_PORT_MAP";    // 포트 → 포트 힌트
    public const string PARTIAL   = "WIRE_PARTIAL";     // 일부 공개 힌트

    public static readonly string[] All =
    {
        COLOR_MAP,
        PORT_MAP,
        PARTIAL
    };
}


public class WirePuzzle : InteractableBase
{
    private const string KEY_WIRE_SEED = "PUZ_WIRE_SEED";
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

            
            int seed;
            if (room.CustomProperties.TryGetValue(KEY_WIRE_SEED, out var seedObj))
                seed = (int)seedObj;
            else
            {
                seed = PhotonNetwork.ServerTimestamp ^ photonView.ViewID;

                room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { KEY_WIRE_SEED, seed }
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
