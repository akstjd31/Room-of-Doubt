using UnityEngine;

public class Drawer : InteractableBase
{
    private Animator anim;
    private bool isOpen;

    private void Awake()
    {
        anim = this.transform.parent.GetComponent<Animator>();
        isOpen = false;
    }

    public override void Interact(int actorNumber)
    {
        isOpen = !isOpen;
        anim.SetBool("IsOpen", isOpen);
    }
}
