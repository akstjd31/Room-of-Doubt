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
        if (keyPad == null)
        {
            isOpen = !isOpen;
            anim.SetBool("IsOpen", isOpen);
            return;
        }
        else
        {
            if (keyPad.IsSolved())
            {
                if (prompt.Length >= 1) prompt = "";

                if (!Cursor.visible)
                {
                    isOpen = !isOpen;
                    anim.SetBool("IsOpen", isOpen);
                }
            }
        }
        
        if (!Cursor.visible)
            UIManager.Instance.ShowMessage(prompt);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
