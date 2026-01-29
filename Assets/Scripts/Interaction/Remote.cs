using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Remote : InteractableBase
{
    [SerializeField] private GameObject tapeObj;
    public override void Interact(int actorNumber)
    {
        if (needItem != null)
        {
            needItem = null;
            tapeObj.SetActive(false);
            return;
        }
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
