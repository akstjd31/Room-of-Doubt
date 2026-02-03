using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LiftObject : InteractableBase
{
    private AudioSource audiosource;
    [SerializeField] private AudioClip putDownSound;
    
    public override void Interact(int actorNumber)
    {
        // 로컬만 실행
        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
            return;

        float delta = isInteracting ? 0.3f : -0.3f;
        transform.position += new Vector3(0f, delta, 0f);

        if (!isInteracting)
            audiosource.PlayOneShot(putDownSound);
    }

    protected override IEnumerator InitRoutine()
    {
        audiosource = this.GetComponent<AudioSource>();
        yield break;
    }
}
