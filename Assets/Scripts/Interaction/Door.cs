using System.Collections;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

public class Door : InteractableBase
{
    private Animator anim;
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] doorSounds;
    private bool isOpen;
    [SerializeField] private KeyPad keyPad;

    private void Awake()
    {
        anim = this.transform.parent.GetComponent<Animator>();
        audioSource = this.GetComponent<AudioSource>();
        if (anim == null) anim = this.transform.GetComponent<Animator>();
        isOpen = false;

        if (keyPad == null)
            keyPad = GetComponentInParent<KeyPad>() ?? GetComponentInChildren<KeyPad>(true);
    }

    public override void Interact(int actorNumber)
    {
        if (keyPad != null)
        {
            if (keyPad.IsSolved()) keyPad = null;
            else ShowLocalPrompt(actorNumber);

            return;
        }

        if (requiredItem != null)
            requiredItem = null;
            
        isOpen = !isOpen;
        anim.SetBool("IsOpen", isOpen);
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
    
    private void OnPlayDoorOpenSoundEvent()
    {
        audioSource.PlayOneShot(doorSounds[0]);
    }

    private void OnPlayDoorCloseSoundEvent()
    {
        audioSource.PlayOneShot(doorSounds[1]);
    }
}
