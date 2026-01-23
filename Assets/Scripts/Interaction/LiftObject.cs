using System.Collections;
using UnityEngine;

public class LiftObject : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        this.transform.position = new Vector3
        (
            this.transform.position.x,
            isInteracting ? this.transform.position.y + 0.3f : this.transform.position.y - 0.3f,
            this.transform.position.z
        );
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
