using UnityEngine;

public class Door : InteractableBase
{
    private Animator anim;
    private bool isOpen;

    private void Awake()
    {
        anim = this.GetComponent<Animator>();
        isOpen = false;
    }

    public override void Interact(int actorNumber)
    {
        if (requiredItem != null)
            requiredItem = null;
            
        isOpen = !isOpen;
        anim.SetBool("IsOpen", isOpen);
    }
}
