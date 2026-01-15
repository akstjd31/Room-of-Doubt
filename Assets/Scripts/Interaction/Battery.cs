using UnityEngine;

public class Battery : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        this.gameObject.SetActive(false);
    }
}
