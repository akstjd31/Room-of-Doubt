using System.Collections;
using UnityEngine;

public class CameraSwitcherObject : InteractableBase
{
    public override void Interact(int actorNumber)
    {

    }

    protected override IEnumerator InitRoutine()
    {
        while (playerCamCtrl == null)
        {
            playerCamCtrl = FindLocalCamCtrl();
            if (playerCamCtrl == null)
                yield return null; // 다음 프레임
        }
    }
}
