using System.Collections;
using UnityEngine;

public class Drawer : InteractableBase
{
    private Animator anim;
    private bool isOpen;
    [SerializeField] private KeyPad keyPad;

    private void Awake()
    {
        anim = this.transform.parent.GetComponent<Animator>();
        isOpen = false;

        if (keyPad == null)
            keyPad = GetComponentInParent<KeyPad>() ?? GetComponentInChildren<KeyPad>(true);
    }

    public override void Interact(int actorNumber)
    {
        if (keyPad != null)
        {
            if (keyPad.IsSolved())
            {
                isOpen = !isOpen;
                anim.SetBool("IsOpen", isOpen);
            }
        }
        else
        {
            isOpen = !isOpen;
            anim.SetBool("IsOpen", isOpen);
        }
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
