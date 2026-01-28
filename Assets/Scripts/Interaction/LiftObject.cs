using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LiftObject : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        // 로컬만 실행
        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
            return;

        float delta = isInteracting ? 0.3f : -0.3f;
        transform.position += new Vector3(0f, delta, 0f);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
