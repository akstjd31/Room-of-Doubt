using System.Collections;
using UnityEngine;

public class Door : InteractableBase
{
    private Animator anim;
    private bool isOpen;
    [SerializeField] private KeyPad keyPad;

    private void Awake()
    {
        anim = this.transform.parent.GetComponent<Animator>();
        if (anim == null) anim = this.transform.GetComponent<Animator>();
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
                if (requiredItem != null)
                    requiredItem = null;

                isOpen = !isOpen;
                anim.SetBool("IsOpen", isOpen);
            }
            else
            {
                ShowLocalPrompt(actorNumber);
            }
        }
        else
        {
            if (RequiredItem != null)
                ShowLocalPrompt(actorNumber);

            isOpen = !isOpen;
            anim.SetBool("IsOpen", isOpen);
        }
    }

    private void ShowLocalPrompt(int actorNumber)
    {
        if (Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            UIManager.Instance.ShowMessage(prompt);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
