using System.Collections;
using Photon.Pun;
using UnityEngine;

public class RewardObject : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.Destroy(this.gameObject);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
