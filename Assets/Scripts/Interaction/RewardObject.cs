using System.Collections;
using Photon.Pun;
using UnityEngine;

public class RewardObject : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        if (requiredItem != null) rewardItem = null;
        
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            SoundManager.Instance.PlayItemPickUpSound();
        
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(this.gameObject);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
