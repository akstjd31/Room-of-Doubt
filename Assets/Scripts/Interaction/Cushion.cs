using System.Collections;
using UnityEngine;

public class Cushion : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        this.transform.position = new Vector3
        (
            this.transform.position.x,
            isInteracting ? this.transform.position.y + 1 : this.transform.position.y - 1,
            this.transform.position.z
        );
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
