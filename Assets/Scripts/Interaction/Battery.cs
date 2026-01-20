using System.Collections;
using UnityEngine;

public class Battery : InteractableBase
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
