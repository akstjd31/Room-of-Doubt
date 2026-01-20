using System.Collections;
using Photon.Pun;
using UnityEngine;

public class CubeObj : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        this.gameObject.SetActive(false);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
